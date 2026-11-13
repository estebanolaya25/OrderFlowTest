-- sqlcmd -S "localhost,1433" -U sa -P "..." -C -b -I -f 65001 -d OrderFlowInventoryDB -i 02_permisos.sql
-- Mínimo privilegio: el servicio no puede borrar ni alterar el esquema.

GRANT SELECT, UPDATE ON inv.Stock TO [orderflow-inventory];
GRANT SELECT, INSERT ON inv.Reservas TO [orderflow-inventory];
GRANT SELECT, INSERT ON inv.MensajesProcesados TO [orderflow-inventory];
GO
