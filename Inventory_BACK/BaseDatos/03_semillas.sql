-- sqlcmd -S "localhost,1433" -U sa -P "..." -C -b -I -f 65001 -d OrderFlowInventoryDB -i 03_semillas.sql
-- Stock inicial. Solo inserta los SKU que no existen: al reiniciar el sistema
-- no se pisa el stock que ya se descontó (los estados persisten mientras viva el volumen).
--   ABC-03 tiene poco stock y ABC-04 ninguno, para poder demostrar el rechazo.

SET XACT_ABORT ON;
GO

MERGE inv.Stock AS destino
USING (VALUES
    (N'ABC-01', N'Teclado mecánico',     100),
    (N'ABC-02', N'Mouse inalámbrico',     50),
    (N'ABC-03', N'Monitor 24 pulgadas',    5),
    (N'ABC-04', N'Audífonos Bluetooth',    0)
) AS origen (Sku, Nombre, Disponible)
ON destino.Sku = origen.Sku
WHEN NOT MATCHED THEN
    INSERT (Sku, Nombre, Disponible) VALUES (origen.Sku, origen.Nombre, origen.Disponible);
GO
