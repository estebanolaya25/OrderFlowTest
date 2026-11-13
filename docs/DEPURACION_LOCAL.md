# Depuración local

Cómo ejecutar y depurar Orders API, Inventory y el panel desde Visual Studio / `ng serve`, usando SQL Server y RabbitMQ de Docker, **sin afectar** los servicios que ya corren en contenedores.

## Cómo se aíslan

SQL Server y RabbitMQ son los mismos contenedores; el aislamiento es por base de datos, por *virtual host* y por puertos:

| | Docker (no se modifica) | Local / depuración |
|---|---|---|
| Bases de datos | `OrderFlowOrdersDB`, `OrderFlowInventoryDB` | `OrderFlowOrdersDB_Local`, `OrderFlowInventoryDB_Local` |
| RabbitMQ | vhost `/` | vhost `orderflow-local` |
| Orders API | :5201 | :5301 |
| Inventory | :5202 | :5302 |
| Panel | :4200 | :4201 |
| Configuración | variables de entorno en `docker-compose.yml` | `dotnet user-secrets` (fuera del repositorio) |

El vhost separado importa: si los servicios locales usaran el vhost `/`, competirían con los de Docker por las mismas colas y cada mensaje lo procesaría uno u otro al azar.

## Requisitos

- Docker Desktop
- SDK de .NET 9 y Visual Studio 2022 actualizado (los perfiles de inicio múltiple del archivo `.slnLaunch` requieren una versión reciente; si no aparece el perfil, configurar *Varios proyectos de inicio* con Orders_Api e Inventory_Api)
- Node 22 (solo para el panel)

## 1. Preparar el entorno

Desde la raíz del repositorio:

```powershell
powershell -ExecutionPolicy Bypass -File .\iniciar-local.ps1
```

El script:

1. Levanta solo `sqlserver` y `rabbitmq` en Docker (si ya corren, no los reinicia).
2. Crea las bases `*_Local` con el servicio `db-init-local` (perfil `local` de compose, que no forma parte de un `docker compose up` normal).
3. Crea el vhost `orderflow-local` y da permisos al usuario de RabbitMQ.
4. Guarda la configuración de cada API en `dotnet user-secrets`: cadena de conexión a la base `_Local`, `localhost:5673` y el vhost local.

Se puede ejecutar varias veces sin problema. Si hay un `.env` en la raíz, usa sus contraseñas; si no, las mismas por defecto de `docker-compose.yml`.

## 2. Depurar en Visual Studio

1. Abrir `OrderFlow.sln`.
2. En el selector de proyecto de inicio, elegir **"Backend local (Orders + Inventory)"** (definido en `OrderFlow.slnLaunch`).
3. **F5.** Los breakpoints funcionan en las dos APIs a la vez y se abre Swagger de cada una:
   - Orders: http://localhost:5301/swagger
   - Inventory: http://localhost:5302/swagger

## 3. Panel (opcional)

```powershell
cd OrderFlow_FRONT
npm ci        # solo la primera vez
npm start     # http://localhost:4201
```

`ng serve` reenvía `/api` a `http://localhost:5301` (`proxy.conf.json`).

## Sin Visual Studio

```powershell
powershell -ExecutionPolicy Bypass -File .\iniciar-local.ps1 -Ejecutar
```

Compila la solución y abre una ventana por cada API y otra para el panel. Para detener, cerrar las ventanas.

## Dónde ver los datos locales

- **SQL Server:** `localhost,14333` (usuario `sa`), bases `OrderFlowOrdersDB_Local` y `OrderFlowInventoryDB_Local`.
- **RabbitMQ:** http://localhost:15673. En *Queues* filtrar por el vhost `orderflow-local`.

## Problemas comunes

| Síntoma | Causa / solución |
|---|---|
| Al arrancar: `Falta configurar ConnectionStrings:SQLConnectionString` | No se ejecutó `iniciar-local.ps1`, o el perfil no es `Development` (los user-secrets solo se cargan en Development). |
| `/health/ready` responde 503 o *Degraded* | SQL Server (503) o RabbitMQ (*Degraded*) no están corriendo: `docker compose up -d sqlserver rabbitmq`. |
| Puerto 5301/5302 en uso | Quedó una instancia anterior abierta; cerrarla o detener el proceso `Orders_Api` / `Inventory_Api`. |
| Quiero empezar con stock limpio | Borrar las bases `*_Local` desde SSMS y volver a ejecutar `iniciar-local.ps1`. |
| Ver los secretos guardados | `dotnet user-secrets list --project Orders_BACK\Orders_Api` |
