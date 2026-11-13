-- sqlcmd -S "localhost,1433" -U sa -P "..." -C -b -I -f 65001 -d OrderFlowOrdersDB -i 02_permisos.sql
-- Mínimo privilegio: el servicio no puede borrar ni alterar el esquema.

GRANT SELECT ON ped.Productos TO [orderflow-orders];
GRANT SELECT, INSERT, UPDATE ON ped.Pedidos TO [orderflow-orders];
GRANT SELECT, INSERT, UPDATE ON ped.Outbox TO [orderflow-orders];
GRANT SELECT, INSERT ON ped.MensajesProcesados TO [orderflow-orders];
GO
