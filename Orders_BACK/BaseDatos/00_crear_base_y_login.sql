-- sqlcmd -S "localhost,1433" -U sa -P "..." -C -b -I -f 65001 -v ClaveLogin="..." -i 00_crear_base_y_login.sql
-- Re-ejecutable: db-init lo corre en cada "docker compose up".

IF DB_ID(N'OrderFlowOrdersDB') IS NULL
    CREATE DATABASE OrderFlowOrdersDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'orderflow-orders')
    CREATE LOGIN [orderflow-orders] WITH PASSWORD = N'$(ClaveLogin)', CHECK_POLICY = ON, DEFAULT_DATABASE = OrderFlowOrdersDB;
GO

USE OrderFlowOrdersDB;
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'orderflow-orders')
    CREATE USER [orderflow-orders] FOR LOGIN [orderflow-orders];
GO
