-- sqlcmd -S "localhost,1433" -U sa -P "..." -C -b -I -f 65001 -v ClaveLogin="..." -i 00_crear_base_y_login.sql
-- Re-ejecutable: db-init lo corre en cada "docker compose up".

IF DB_ID(N'OrderFlowInventoryDB') IS NULL
    CREATE DATABASE OrderFlowInventoryDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'orderflow-inventory')
    CREATE LOGIN [orderflow-inventory] WITH PASSWORD = N'$(ClaveLogin)', CHECK_POLICY = ON, DEFAULT_DATABASE = OrderFlowInventoryDB;
GO

USE OrderFlowInventoryDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'orderflow-inventory')
    CREATE USER [orderflow-inventory] FOR LOGIN [orderflow-inventory];
GO
