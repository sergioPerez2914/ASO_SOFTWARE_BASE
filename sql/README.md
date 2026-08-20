# Scripts SQL

Scripts generados desde las migraciones de EF Core, para aplicar el esquema **sin necesitar el
tooling de .NET** (útil si trabajas en SSMS). Son equivalentes a `dotnet ef database update`.

Se regeneran desde `ASO.Desktop/` con `dotnet ef migrations script`; los comandos exactos están
al final de este archivo.

## Cuál usar

| Script | Cuándo | Qué hace |
|---|---|---|
| **01-esquema-completo-desde-cero.sql** | Base de datos **nueva y vacía** | Crea todo el esquema: las 8 migraciones seguidas |
| **02-actualizar-idempotente.sql** | **Cualquier base existente**, o si no sabes en qué estado está | Comprueba qué migraciones faltan y aplica solo esas. Ejecutarlo dos veces no rompe nada |
| **03-solo-fase6.sql** | Base que ya está en `FixStockActualStockMinimoDecimal` | Solo el paso de organización y seguridad |
| **04-revertir-fase6.sql** | Deshacer la Fase 6 | Ver la advertencia de abajo |

**Si dudas, usa el 02.** Es el único que comprueba el estado antes de tocar nada.

## Antes de ejecutar cualquiera

1. **Respalda.** La Fase 6 añade una columna a 21 tablas.
   ```sql
   BACKUP DATABASE [NombreDeLaBase]
   TO DISK = N'C:\Respaldos\pre_Fase6.bak'
   WITH INIT, COMPRESSION, STATS = 10;
   ```
2. Comprueba contra qué servidor estás conectado. Estos scripts no traen cadena de conexión:
   aplican sobre la base que tengas abierta en SSMS.
3. Revisa en qué estado está la base:
   ```sql
   SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
   ```

## Sobre el script 04 (revertir)

> **Borra las cuatro tablas nuevas** — `Organizaciones`, `Usuarios`, `PermisosUsuario` y
> `PeticionesCambio` — con todo lo que tengan dentro. Se pierden **todos los usuarios y sus
> contraseñas**, los ajustes de permisos y las peticiones de cambio: eso no se recupera salvo
> desde un respaldo.
>
> Los datos del negocio (remesas, flota, nómina, finanzas) **no se borran**; solo pierden la
> columna `OrganizacionId`, es decir, a qué núcleo pertenecía cada fila. Si después vuelves a
> aplicar la Fase 6, todo cae otra vez en el núcleo 1.

Al revertir, la aplicación **no arranca**: espera la tabla `Usuarios` para el inicio de sesión.

## Después de aplicar la Fase 6

Índices recomendados. Desde ahora **cada consulta** de la aplicación filtra por `OrganizacionId`,
y la migración no crea ninguno. En los catálogos da igual; en las tablas que crecen por zafra, no:

```sql
CREATE INDEX IX_Remesas_OrganizacionId                ON Remesas(OrganizacionId);
CREATE INDEX IX_ValesCombustible_OrganizacionId       ON ValesCombustible(OrganizacionId);
CREATE INDEX IX_Jornadas_OrganizacionId               ON Jornadas(OrganizacionId);
CREATE INDEX IX_SalidasInventario_OrganizacionId      ON SalidasInventario(OrganizacionId);
CREATE INDEX IX_EventosOperacion_OrganizacionId       ON EventosOperacion(OrganizacionId);
CREATE INDEX IX_MantenimientoRegistros_OrganizacionId ON MantenimientoRegistros(OrganizacionId);
CREATE INDEX IX_RecargasCombustible_OrganizacionId    ON RecargasCombustible(OrganizacionId);
CREATE INDEX IX_Liquidaciones_OrganizacionId          ON Liquidaciones(OrganizacionId);
CREATE INDEX IX_FacturasCliente_OrganizacionId        ON FacturasCliente(OrganizacionId);
CREATE INDEX IX_FacturasProveedor_OrganizacionId      ON FacturasProveedor(OrganizacionId);
```

Cuando haya volumen real conviene mirar los planes: seguramente salgan mejor índices compuestos
`(OrganizacionId, Fecha)` o `(OrganizacionId, Estado)`, según lo que filtre cada pantalla.

## Comprobar que quedó bien

**¿Se aplicó todo?** Deben salir las ocho migraciones, terminando en `Fase6`.

```sql
SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId;
```

**¿Dónde quedaron las filas?** Todo lo que ya existía debe estar en el núcleo 1, nada en el 0.

```sql
SELECT o.Id, o.Codigo, o.Nombre,
       (SELECT COUNT(*) FROM Remesas      r WHERE r.OrganizacionId = o.Id) AS Remesas,
       (SELECT COUNT(*) FROM ActivosFlota a WHERE a.OrganizacionId = o.Id) AS Flota,
       (SELECT COUNT(*) FROM Empleados    e WHERE e.OrganizacionId = o.Id) AS Empleados,
       (SELECT COUNT(*) FROM Usuarios     u WHERE u.OrganizacionId = o.Id) AS Usuarios
FROM Organizaciones o
ORDER BY o.Id;
```

**¿Hay filas huérfanas?** Ésta es la importante: recorre todas las tablas con `OrganizacionId` y
busca filas que apunten a un núcleo que no existe. Esas son **invisibles para la aplicación** y
nadie las echa de menos hasta que falte un pago. Lo esperado es cero en todas.
Requiere SQL Server 2017 o superior.

```sql
DECLARE @sql NVARCHAR(MAX);

SELECT @sql = STRING_AGG(CAST(
        'SELECT ' + QUOTENAME(t.name, '''') + ' AS Tabla, COUNT(*) AS Huerfanas FROM '
        + QUOTENAME(t.name) + ' x WHERE NOT EXISTS '
        + '(SELECT 1 FROM Organizaciones o WHERE o.Id = x.OrganizacionId)'
        AS NVARCHAR(MAX)), ' UNION ALL ')
FROM sys.tables  t
JOIN sys.columns c ON c.object_id = t.object_id AND c.name = 'OrganizacionId';

EXEC sp_executesql @sql;
```

## Tres cosas que hay que saber al escribir contra esta base

1. **El aislamiento es de aplicación, no de base de datos.** El filtro por núcleo lo pone EF Core
   en cada consulta. Un `SELECT` en SSMS, una vista, un procedimiento almacenado o un
   `FromSqlRaw` ven **todos los núcleos**. Si escribes reportes en SQL, el
   `WHERE OrganizacionId = @…` lo pones tú.

2. **Un `INSERT` manual sin `OrganizacionId` cae en el núcleo 1, sin error y sin aviso.** Las 21
   columnas quedaron con `DEFAULT 1` — eso es lo que salvó los datos existentes al migrar, pero
   significa que un script de carga que olvide la columna mete las filas en el núcleo equivocado.
   Al cargar maestros, indícala siempre.

3. **Una entidad nueva sin `IDeOrganizacion` queda compartida entre todos los núcleos.** El filtro
   y el estampado automáticos se activan por implementar esa interfaz (una sola propiedad,
   `int OrganizacionId`). No falla: filtra de menos.

## Regenerar estos scripts

Desde `ASO.Desktop/`:

```bash
dotnet ef migrations script 0 Fase6_OrganizacionYSeguridad -o ../sql/01-esquema-completo-desde-cero.sql
dotnet ef migrations script --idempotent                   -o ../sql/02-actualizar-idempotente.sql
dotnet ef migrations script FixStockActualStockMinimoDecimal Fase6_OrganizacionYSeguridad -o ../sql/03-solo-fase6.sql
dotnet ef migrations script Fase6_OrganizacionYSeguridad FixStockActualStockMinimoDecimal -o ../sql/04-revertir-fase6.sql
```

Si no tienes la herramienta: `dotnet tool install --global dotnet-ef --version 8.*`

**Nunca edites una migración ya aplicada** en alguna máquina. Si algo salió mal, se corrige con una
migración nueva encima.
