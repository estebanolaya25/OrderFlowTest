# Prepara el entorno para depurar Orders e Inventory desde Visual Studio, SIN tocar lo que corre en Docker.
#
#   Docker (no se modifica)                     Local / depuración
#   ------------------------------------------  ------------------------------------------------
#   OrderFlowOrdersDB / OrderFlowInventoryDB    OrderFlowOrdersDB_Local / OrderFlowInventoryDB_Local
#   RabbitMQ vhost "/"                          RabbitMQ vhost "orderflow-local" (colas separadas)
#   orders-api :5201  inventory-api :5202       Orders_Api :5301  Inventory_Api :5302
#   frontend :4200                              ng serve :4201
#
# SQL Server y RabbitMQ son los mismos contenedores; el aislamiento es por base de datos y por vhost,
# así los consumidores locales no le quitan mensajes a los de Docker (ni al revés).
#
# Uso:
#   powershell -ExecutionPolicy Bypass -File .\iniciar-local.ps1            # solo prepara (luego F5 en Visual Studio)
#   powershell -ExecutionPolicy Bypass -File .\iniciar-local.ps1 -Ejecutar  # prepara y arranca APIs + front en ventanas

param([switch]$Ejecutar)

$raiz = $PSScriptRoot
$compose = Join-Path $raiz 'docker-compose.yml'

$puertoSql = 14333        # puerto publicado de SQL Server en docker-compose.yml
$puertoRabbit = 5673      # puerto publicado de RabbitMQ en docker-compose.yml
$vhost = 'orderflow-local'
$sufijoBd = '_Local'

# Mismos valores por defecto que docker-compose.yml; si existe un .env, se usan los de ahí.
$config = @{
    MSSQL_SA_PASSWORD     = 'OrderFlow_Sa_2026!'
    ORDERS_DB_PASSWORD    = 'OrderFlow_Orders_2026!'
    INVENTORY_DB_PASSWORD = 'OrderFlow_Inventory_2026!'
    RABBITMQ_USER         = 'orderflow'
    RABBITMQ_PASSWORD     = 'OrderFlow_Rabbit_2026!'
}
$archivoEnv = Join-Path $raiz '.env'
if (Test-Path $archivoEnv) {
    foreach ($linea in Get-Content $archivoEnv) {
        if ($linea -match '^\s*([A-Z_]+)\s*=\s*(.+?)\s*$') { $config[$Matches[1]] = $Matches[2] }
    }
}

function Paso([string]$texto) { Write-Host "`n>> $texto" -ForegroundColor Cyan }

# Los comandos externos (docker, dotnet) se validan por código de salida: en PowerShell 5.1 su salida
# de progreso por stderr no debe tratarse como error.
function Comprobar([string]$mensaje) { if ($LASTEXITCODE -ne 0) { throw $mensaje } }

Paso 'SQL Server y RabbitMQ en Docker (si ya están corriendo, no se reinician)'
docker compose -f $compose up -d --wait sqlserver rabbitmq
Comprobar 'No se pudieron levantar SQL Server y RabbitMQ. ¿Docker Desktop está abierto?'

Paso "Bases de datos de depuración (sufijo $sufijoBd)"
docker compose -f $compose --profile local run --rm db-init-local
Comprobar 'Falló la creación de las bases locales.'

Paso "Virtual host '$vhost' en RabbitMQ"
$vhosts = docker exec orderflow-rabbitmq rabbitmqctl -q list_vhosts name
Comprobar 'No se pudo consultar RabbitMQ.'
if ($vhosts -notcontains $vhost) {
    docker exec orderflow-rabbitmq rabbitmqctl -q add_vhost $vhost
    Comprobar "No se pudo crear el vhost $vhost."
}
docker exec orderflow-rabbitmq rabbitmqctl -q set_permissions -p $vhost $config.RABBITMQ_USER '.*' '.*' '.*'
Comprobar "No se pudieron asignar permisos en $vhost."
Write-Host "   vhost $vhost listo"

# La configuración local va a user-secrets (fuera del repositorio), no a appsettings.json.
Paso 'Configuración local (dotnet user-secrets)'
$servicios = @(
    @{ Nombre = 'Orders';    Proyecto = 'Orders_BACK\Orders_Api';       Puerto = 5301; Base = "OrderFlowOrdersDB$sufijoBd";    Login = 'orderflow-orders';    Clave = $config.ORDERS_DB_PASSWORD },
    @{ Nombre = 'Inventory'; Proyecto = 'Inventory_BACK\Inventory_Api'; Puerto = 5302; Base = "OrderFlowInventoryDB$sufijoBd"; Login = 'orderflow-inventory'; Clave = $config.INVENTORY_DB_PASSWORD }
)
foreach ($s in $servicios) {
    $proyecto = Join-Path $raiz $s.Proyecto
    $secretos = [ordered]@{
        'ConnectionStrings:SQLConnectionString' = "Server=localhost,$puertoSql;Database=$($s.Base);User Id=$($s.Login);Password=$($s.Clave);TrustServerCertificate=True;"
        'RabbitMQ:Host'        = 'localhost'
        'RabbitMQ:Puerto'      = "$puertoRabbit"
        'RabbitMQ:VirtualHost' = $vhost
        'RabbitMQ:Usuario'     = $config.RABBITMQ_USER
        'RabbitMQ:Password'    = $config.RABBITMQ_PASSWORD
    }
    foreach ($clave in $secretos.Keys) {
        dotnet user-secrets set $clave $secretos[$clave] --project $proyecto | Out-Null
        Comprobar "No se pudo guardar $clave en los user-secrets de $($s.Nombre)."
    }
    Write-Host ("   {0,-10} -> {1} / vhost {2}" -f $s.Nombre, $s.Base, $vhost)
}

if ($Ejecutar) {
    Paso 'Compilando la solución'
    dotnet build (Join-Path $raiz 'OrderFlow.sln') -v q --nologo
    Comprobar 'La compilación falló.'

    foreach ($s in $servicios) {
        $ruta = Join-Path $raiz $s.Proyecto
        $comando = "`$Host.UI.RawUI.WindowTitle = '$($s.Nombre) :$($s.Puerto)'; dotnet run --no-build --launch-profile http --project '$ruta'"
        Start-Process powershell -ArgumentList '-NoExit', '-Command', $comando
    }

    $front = Join-Path $raiz 'OrderFlow_FRONT'
    $comandoFront = "`$Host.UI.RawUI.WindowTitle = 'Front :4201'; Set-Location '$front'; if (-not (Test-Path node_modules)) { npm ci }; npm start"
    Start-Process powershell -ArgumentList '-NoExit', '-Command', $comandoFront
}

Write-Host ''
Write-Host 'Entorno local listo. Docker sigue intacto (4200 / 5201 / 5202).' -ForegroundColor Green
if (-not $Ejecutar) {
    Write-Host '  Visual Studio: abrir OrderFlow.sln, elegir "Backend local (Orders + Inventory)" en el selector de inicio y F5.'
    Write-Host '  Frontend:      cd OrderFlow_FRONT; npm start'
}
Write-Host '  Orders    Swagger  http://localhost:5301/swagger'
Write-Host '  Inventory Swagger  http://localhost:5302/swagger'
Write-Host '  Panel web          http://localhost:4201'
Write-Host "  RabbitMQ           http://localhost:15673  (vhost $vhost)"
Write-Host "  SQL Server         localhost,$puertoSql  (bases *$sufijoBd)"
