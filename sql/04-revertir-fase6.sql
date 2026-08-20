BEGIN TRANSACTION;
GO

DROP TABLE [Organizaciones];
GO

DROP TABLE [PermisosUsuario];
GO

DROP TABLE [PeticionesCambio];
GO

DROP TABLE [Usuarios];
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ValesCombustible]') AND [c].[name] = N'OrganizacionId');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [ValesCombustible] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [ValesCombustible] DROP COLUMN [OrganizacionId];
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Tarifas]') AND [c].[name] = N'OrganizacionId');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Tarifas] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Tarifas] DROP COLUMN [OrganizacionId];
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TanquesCombustible]') AND [c].[name] = N'OrganizacionId');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [TanquesCombustible] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [TanquesCombustible] DROP COLUMN [OrganizacionId];
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[SalidasInventario]') AND [c].[name] = N'OrganizacionId');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [SalidasInventario] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [SalidasInventario] DROP COLUMN [OrganizacionId];
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Remesas]') AND [c].[name] = N'OrganizacionId');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Remesas] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Remesas] DROP COLUMN [OrganizacionId];
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ReglasMantenimiento]') AND [c].[name] = N'OrganizacionId');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [ReglasMantenimiento] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [ReglasMantenimiento] DROP COLUMN [OrganizacionId];
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RecargasCombustible]') AND [c].[name] = N'OrganizacionId');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [RecargasCombustible] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [RecargasCombustible] DROP COLUMN [OrganizacionId];
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Proveedores]') AND [c].[name] = N'OrganizacionId');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [Proveedores] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [Proveedores] DROP COLUMN [OrganizacionId];
GO

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PersonalCampo]') AND [c].[name] = N'OrganizacionId');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [PersonalCampo] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [PersonalCampo] DROP COLUMN [OrganizacionId];
GO

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Nucleos]') AND [c].[name] = N'OrganizacionId');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Nucleos] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [Nucleos] DROP COLUMN [OrganizacionId];
GO

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MantenimientoRegistros]') AND [c].[name] = N'OrganizacionId');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [MantenimientoRegistros] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [MantenimientoRegistros] DROP COLUMN [OrganizacionId];
GO

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Liquidaciones]') AND [c].[name] = N'OrganizacionId');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Liquidaciones] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [Liquidaciones] DROP COLUMN [OrganizacionId];
GO

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Jornadas]') AND [c].[name] = N'OrganizacionId');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Jornadas] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [Jornadas] DROP COLUMN [OrganizacionId];
GO

DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inventarios]') AND [c].[name] = N'OrganizacionId');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Inventarios] DROP CONSTRAINT [' + @var13 + '];');
ALTER TABLE [Inventarios] DROP COLUMN [OrganizacionId];
GO

DECLARE @var14 sysname;
SELECT @var14 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Fincas]') AND [c].[name] = N'OrganizacionId');
IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [Fincas] DROP CONSTRAINT [' + @var14 + '];');
ALTER TABLE [Fincas] DROP COLUMN [OrganizacionId];
GO

DECLARE @var15 sysname;
SELECT @var15 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FacturasProveedor]') AND [c].[name] = N'OrganizacionId');
IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [FacturasProveedor] DROP CONSTRAINT [' + @var15 + '];');
ALTER TABLE [FacturasProveedor] DROP COLUMN [OrganizacionId];
GO

DECLARE @var16 sysname;
SELECT @var16 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FacturasCliente]') AND [c].[name] = N'OrganizacionId');
IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [FacturasCliente] DROP CONSTRAINT [' + @var16 + '];');
ALTER TABLE [FacturasCliente] DROP COLUMN [OrganizacionId];
GO

DECLARE @var17 sysname;
SELECT @var17 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[EventosOperacion]') AND [c].[name] = N'OrganizacionId');
IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [EventosOperacion] DROP CONSTRAINT [' + @var17 + '];');
ALTER TABLE [EventosOperacion] DROP COLUMN [OrganizacionId];
GO

DECLARE @var18 sysname;
SELECT @var18 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Empleados]') AND [c].[name] = N'OrganizacionId');
IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [Empleados] DROP CONSTRAINT [' + @var18 + '];');
ALTER TABLE [Empleados] DROP COLUMN [OrganizacionId];
GO

DECLARE @var19 sysname;
SELECT @var19 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ConceptosNomina]') AND [c].[name] = N'OrganizacionId');
IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [ConceptosNomina] DROP CONSTRAINT [' + @var19 + '];');
ALTER TABLE [ConceptosNomina] DROP COLUMN [OrganizacionId];
GO

DECLARE @var20 sysname;
SELECT @var20 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ActivosFlota]') AND [c].[name] = N'OrganizacionId');
IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [ActivosFlota] DROP CONSTRAINT [' + @var20 + '];');
ALTER TABLE [ActivosFlota] DROP COLUMN [OrganizacionId];
GO

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20260820172038_Fase6_OrganizacionYSeguridad';
GO

COMMIT;
GO

