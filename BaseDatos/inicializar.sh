#!/bin/bash
# Crea las bases, logins, esquemas, permisos y semillas de los dos servicios.
# Lo ejecuta el contenedor db-init en cada "docker compose up": todos los scripts son re-ejecutables.
# SUFIJO_BD (opcional) crea un juego de bases aparte, p. ej. "_Local" para depurar desde Visual Studio
# sin tocar los datos de los servicios que corren en Docker.
set -euo pipefail

SUFIJO="${SUFIJO_BD:-}"
SQLCMD=(/opt/mssql-tools18/bin/sqlcmd -S "${DB_HOST:-sqlserver},1433" -U sa -P "$MSSQL_SA_PASSWORD" -C -b -I -f 65001)

ejecutar_servicio() {
    local carpeta="$1" base="$2" clave="$3"
    echo ">> $base"
    "${SQLCMD[@]}" -v BaseDatos="$base" ClaveLogin="$clave" -i "$carpeta/00_crear_base_y_login.sql"
    for script in 01_esquema.sql 02_permisos.sql 03_semillas.sql; do
        echo "   $script"
        "${SQLCMD[@]}" -d "$base" -i "$carpeta/$script"
    done
}

ejecutar_servicio /scripts/orders    "OrderFlowOrdersDB${SUFIJO}"    "$ORDERS_DB_PASSWORD"
ejecutar_servicio /scripts/inventory "OrderFlowInventoryDB${SUFIJO}" "$INVENTORY_DB_PASSWORD"

echo ">> Bases de datos listas."
