-- sqlcmd -S "localhost,1433" -U sa -P "..." -C -b -I -f 65001 -d OrderFlowOrdersDB -i 03_semillas.sql
-- Catálogo inicial. Debe coincidir con los SKU de Inventory_BACK/BaseDatos/03_semillas.sql.
-- MERGE: re-ejecutable, no duplica filas.

SET XACT_ABORT ON;
GO

MERGE ped.Productos AS destino
USING (VALUES
    (N'ABC-01', N'Teclado mecánico'),
    (N'ABC-02', N'Mouse inalámbrico'),
    (N'ABC-03', N'Monitor 24 pulgadas'),
    (N'ABC-04', N'Audífonos Bluetooth')
) AS origen (Sku, Nombre)
ON destino.Sku = origen.Sku
WHEN MATCHED AND destino.Nombre <> origen.Nombre THEN
    UPDATE SET Nombre = origen.Nombre
WHEN NOT MATCHED THEN
    INSERT (Sku, Nombre) VALUES (origen.Sku, origen.Nombre);
GO
