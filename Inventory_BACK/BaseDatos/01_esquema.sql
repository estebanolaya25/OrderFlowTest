-- sqlcmd -S "localhost,1433" -U sa -P "..." -C -b -I -f 65001 -d OrderFlowInventoryDB -i 01_esquema.sql
-- Re-ejecutable: cada objeto se crea solo si no existe, así un segundo "docker compose up" no falla.

SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF SCHEMA_ID(N'inv') IS NULL
    EXEC (N'CREATE SCHEMA inv');
GO

BEGIN TRANSACTION;

-- El CHECK es la última barrera contra el sobreventa: el descuento se hace con
-- UPDATE ... WHERE Disponible >= @Cantidad, atómico aunque haya consumidores concurrentes.
IF OBJECT_ID(N'inv.Stock', N'U') IS NULL
CREATE TABLE inv.Stock (
    Sku            NVARCHAR(50)  NOT NULL,
    Nombre         NVARCHAR(150) NOT NULL,
    Disponible     INT           NOT NULL,
    ActualizadoEn  DATETIME2     NOT NULL CONSTRAINT DF_Stock_ActualizadoEn DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Stock PRIMARY KEY (Sku),
    CONSTRAINT CK_Stock_Disponible CHECK (Disponible >= 0)
);

-- Resultado de cada pedido procesado (uno por pedido). Si el evento llega repetido, el consumidor
-- no toca el stock: lee este resultado y lo vuelve a publicar, por si la publicación anterior falló.
-- Sin FK a inv.Stock: un SKU desconocido también se registra, como Rechazado.
IF OBJECT_ID(N'inv.Reservas', N'U') IS NULL
CREATE TABLE inv.Reservas (
    PedidoId   UNIQUEIDENTIFIER NOT NULL,
    MensajeId  UNIQUEIDENTIFIER NOT NULL,
    Sku        NVARCHAR(50)     NOT NULL,
    Cantidad   INT              NOT NULL,
    Resultado  VARCHAR(20)      NOT NULL,
    Motivo     NVARCHAR(300)    NULL,
    CreadoEn   DATETIME2        NOT NULL CONSTRAINT DF_Reservas_CreadoEn DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Reservas PRIMARY KEY (PedidoId),
    CONSTRAINT UQ_Reservas_Mensaje UNIQUE (MensajeId),
    CONSTRAINT CK_Reservas_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_Reservas_Resultado CHECK (Resultado IN ('Reservado', 'Rechazado'))
);

-- Idempotencia del consumidor de OrderCreated: la PK sobre MensajeId hace que el segundo
-- INSERT del mismo evento falle dentro de la transacción y el stock no se descuente dos veces.
IF OBJECT_ID(N'inv.MensajesProcesados', N'U') IS NULL
CREATE TABLE inv.MensajesProcesados (
    MensajeId    UNIQUEIDENTIFIER NOT NULL,
    TipoMensaje  VARCHAR(100)     NOT NULL,
    ProcesadoEn  DATETIME2        NOT NULL CONSTRAINT DF_Mensajes_ProcesadoEn DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_MensajesProcesados PRIMARY KEY (MensajeId)
);

COMMIT;
GO
