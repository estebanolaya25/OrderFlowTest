-- sqlcmd -S "localhost,1433" -U sa -P "..." -C -b -I -f 65001 -d OrderFlowOrdersDB -i 01_esquema.sql
-- Re-ejecutable: cada objeto se crea solo si no existe, así un segundo "docker compose up" no falla.

SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF SCHEMA_ID(N'ped') IS NULL
    EXEC (N'CREATE SCHEMA ped');
GO

BEGIN TRANSACTION;

-- Copia de solo lectura del catálogo: permite validar el SKU al crear el pedido sin llamar
-- de forma síncrona a Inventory. Trade-off documentado en el README.
IF OBJECT_ID(N'ped.Productos', N'U') IS NULL
CREATE TABLE ped.Productos (
    Sku     NVARCHAR(50)  NOT NULL,
    Nombre  NVARCHAR(150) NOT NULL,
    Activo  BIT           NOT NULL CONSTRAINT DF_Productos_Activo DEFAULT (1),
    CONSTRAINT PK_Productos PRIMARY KEY (Sku)
);

IF OBJECT_ID(N'ped.Pedidos', N'U') IS NULL
CREATE TABLE ped.Pedidos (
    PedidoId       UNIQUEIDENTIFIER NOT NULL,
    ClienteNombre  NVARCHAR(150)    NOT NULL,
    Sku            NVARCHAR(50)     NOT NULL,
    Cantidad       INT              NOT NULL,
    Estado         VARCHAR(20)      NOT NULL CONSTRAINT DF_Pedidos_Estado DEFAULT ('Pending'),
    MotivoRechazo  NVARCHAR(300)    NULL,
    CreadoEn       DATETIME2        NOT NULL CONSTRAINT DF_Pedidos_CreadoEn DEFAULT (SYSUTCDATETIME()),
    ActualizadoEn  DATETIME2        NOT NULL CONSTRAINT DF_Pedidos_ActualizadoEn DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Pedidos PRIMARY KEY (PedidoId),
    CONSTRAINT FK_Pedidos_Producto FOREIGN KEY (Sku) REFERENCES ped.Productos (Sku),
    CONSTRAINT CK_Pedidos_Cliente CHECK (LEN(TRIM(ClienteNombre)) > 0),
    CONSTRAINT CK_Pedidos_Cantidad CHECK (Cantidad BETWEEN 1 AND 100),
    CONSTRAINT CK_Pedidos_Estado CHECK (Estado IN ('Pending', 'Confirmed', 'Rejected'))
);

-- Patrón Outbox: el evento se guarda en la misma transacción que el pedido
-- y PublicadorOutboxService lo publica después en RabbitMQ (si el broker está caído, se reintenta).
IF OBJECT_ID(N'ped.Outbox', N'U') IS NULL
CREATE TABLE ped.Outbox (
    OutboxId     UNIQUEIDENTIFIER NOT NULL,
    Tipo         VARCHAR(100)     NOT NULL,
    Contenido    NVARCHAR(MAX)    NOT NULL,
    CreadoEn     DATETIME2        NOT NULL CONSTRAINT DF_Outbox_CreadoEn DEFAULT (SYSUTCDATETIME()),
    PublicadoEn  DATETIME2        NULL,
    Intentos     INT              NOT NULL CONSTRAINT DF_Outbox_Intentos DEFAULT (0),
    UltimoError  NVARCHAR(500)    NULL,
    CONSTRAINT PK_Outbox PRIMARY KEY (OutboxId)
);

-- Idempotencia de los consumidores de StockReservado / StockRechazado.
IF OBJECT_ID(N'ped.MensajesProcesados', N'U') IS NULL
CREATE TABLE ped.MensajesProcesados (
    MensajeId    UNIQUEIDENTIFIER NOT NULL,
    TipoMensaje  VARCHAR(100)     NOT NULL,
    ProcesadoEn  DATETIME2        NOT NULL CONSTRAINT DF_Mensajes_ProcesadoEn DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_MensajesProcesados PRIMARY KEY (MensajeId)
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Pedidos_CreadoEn' AND object_id = OBJECT_ID(N'ped.Pedidos'))
    CREATE INDEX IX_Pedidos_CreadoEn ON ped.Pedidos (CreadoEn DESC);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Outbox_Pendientes' AND object_id = OBJECT_ID(N'ped.Outbox'))
    CREATE INDEX IX_Outbox_Pendientes ON ped.Outbox (CreadoEn) WHERE PublicadoEn IS NULL;

COMMIT;
GO
