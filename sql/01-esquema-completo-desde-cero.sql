IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Empleados] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(150) NOT NULL,
    [Cedula] nvarchar(20) NOT NULL,
    [Cargo] nvarchar(100) NOT NULL,
    [Activo] bit NOT NULL,
    CONSTRAINT [PK_Empleados] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Inventarios] (
    [Codigo] nvarchar(30) NOT NULL,
    [Nombre] nvarchar(150) NOT NULL,
    [Categoria] nvarchar(100) NOT NULL,
    [Unidad] nvarchar(20) NOT NULL,
    [Ubicacion] nvarchar(50) NOT NULL,
    [StockActual] decimal(18,2) NOT NULL,
    [StockMinimo] decimal(18,2) NOT NULL,
    [CostoUnitario] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_Inventarios] PRIMARY KEY ([Codigo])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819024621_Baseline00_EmpleadoInventario', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ConceptosNomina] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(150) NOT NULL,
    [Tipo] int NOT NULL,
    [Activo] bit NOT NULL,
    CONSTRAINT [PK_ConceptosNomina] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Nucleos] (
    [Id] int NOT NULL IDENTITY,
    [Codigo] nvarchar(30) NOT NULL,
    [Nombre] nvarchar(150) NOT NULL,
    CONSTRAINT [PK_Nucleos] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Proveedores] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(150) NOT NULL,
    [Rif] nvarchar(30) NOT NULL,
    [Telefono] nvarchar(30) NOT NULL,
    [Notas] nvarchar(500) NOT NULL,
    [Activo] bit NOT NULL,
    CONSTRAINT [PK_Proveedores] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [TanquesCombustible] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(150) NOT NULL,
    [CapacidadL] decimal(18,2) NOT NULL,
    [ExistenciaL] decimal(18,2) NOT NULL,
    [Activo] bit NOT NULL,
    CONSTRAINT [PK_TanquesCombustible] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819025032_Fase1_CatalogosSimples', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ActivosFlota] (
    [Id] int NOT NULL IDENTITY,
    [Codigo] nvarchar(30) NOT NULL,
    [Tipo] int NOT NULL,
    [Marca] nvarchar(100) NOT NULL,
    [Modelo] nvarchar(100) NOT NULL,
    [Anio] int NOT NULL,
    [Placa] nvarchar(20) NOT NULL,
    [Descripcion] nvarchar(200) NOT NULL,
    [HorometroHoras] decimal(18,2) NULL,
    [OdometroKm] decimal(18,2) NULL,
    [Estado] int NOT NULL,
    [Notas] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_ActivosFlota] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PersonalCampo] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(150) NOT NULL,
    [Cedula] nvarchar(20) NOT NULL,
    [Rol] int NOT NULL,
    [NucleoCodigo] nvarchar(30) NOT NULL,
    [Activo] bit NOT NULL,
    CONSTRAINT [PK_PersonalCampo] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ReglasMantenimiento] (
    [Id] int NOT NULL IDENTITY,
    [Tipo] int NOT NULL,
    [Revision] nvarchar(150) NOT NULL,
    [IntervaloHoras] decimal(18,2) NULL,
    [IntervaloDias] int NULL,
    CONSTRAINT [PK_ReglasMantenimiento] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Tarifas] (
    [Id] int NOT NULL IDENTITY,
    [Concepto] nvarchar(150) NOT NULL,
    [Servicio] int NOT NULL,
    [Ambito] int NOT NULL,
    [Unidad] int NOT NULL,
    [MontoPorUnidad] decimal(18,2) NOT NULL,
    [VigenteDesde] datetime2 NOT NULL,
    [VigenteHasta] datetime2 NULL,
    [Activa] bit NOT NULL,
    [Notas] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_Tarifas] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819025329_Fase2_CatalogosConRelaciones', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [FacturasProveedor] (
    [Id] int NOT NULL IDENTITY,
    [NumeroDocumento] nvarchar(50) NOT NULL,
    [ProveedorId] int NOT NULL,
    [ProveedorNombre] nvarchar(150) NOT NULL,
    [Descripcion] nvarchar(500) NOT NULL,
    [FechaEmision] datetime2 NOT NULL,
    [FechaVencimiento] datetime2 NOT NULL,
    [Monto] decimal(18,2) NOT NULL,
    [Estado] int NOT NULL,
    [FechaPago] datetime2 NULL,
    [MotivoAnulacion] nvarchar(500) NULL,
    [FechaAnulacion] datetime2 NULL,
    [CreadoPorId] int NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    CONSTRAINT [PK_FacturasProveedor] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Jornadas] (
    [Id] int NOT NULL IDENTITY,
    [Fecha] datetime2 NOT NULL,
    [TipoPersonal] int NOT NULL,
    [PersonaId] int NOT NULL,
    [PersonaNombre] nvarchar(150) NOT NULL,
    [CargoORol] nvarchar(100) NOT NULL,
    [NucleoCodigo] nvarchar(30) NOT NULL,
    [Turno] int NOT NULL,
    [HoraEntrada] datetime2 NOT NULL,
    [HoraSalida] datetime2 NULL,
    [Observacion] nvarchar(500) NOT NULL,
    [CreadoPorId] int NOT NULL,
    [FechaRegistro] datetime2 NOT NULL,
    CONSTRAINT [PK_Jornadas] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [MantenimientoRegistros] (
    [Id] int NOT NULL IDENTITY,
    [ActivoId] int NOT NULL,
    [ActivoCodigo] nvarchar(30) NOT NULL,
    [ActivoEtiqueta] nvarchar(150) NOT NULL,
    [Fecha] datetime2 NOT NULL,
    [Tipo] int NOT NULL,
    [Descripcion] nvarchar(500) NOT NULL,
    [LecturaUso] decimal(18,2) NULL,
    [RepuestosUsados] nvarchar(1000) NOT NULL,
    [CostoRepuestos] decimal(18,2) NULL,
    [CostoManoObra] decimal(18,2) NULL,
    [RealizadoPor] nvarchar(150) NOT NULL,
    [RemesaId] int NULL,
    [FechaRegistro] datetime2 NOT NULL,
    CONSTRAINT [PK_MantenimientoRegistros] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [RecargasCombustible] (
    [Id] int NOT NULL IDENTITY,
    [Fecha] datetime2 NOT NULL,
    [TanqueId] int NOT NULL,
    [TanqueNombre] nvarchar(150) NOT NULL,
    [Litros] decimal(18,2) NOT NULL,
    [CostoTotal] decimal(18,2) NULL,
    [ProveedorNombre] nvarchar(150) NOT NULL,
    [Notas] nvarchar(500) NOT NULL,
    [CreadoPorId] int NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    CONSTRAINT [PK_RecargasCombustible] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Remesas] (
    [Id] int NOT NULL IDENTITY,
    [FincaId] int NOT NULL,
    [FincaCodigoCam] nvarchar(30) NOT NULL,
    [FincaNombre] nvarchar(150) NOT NULL,
    [LoteNombre] nvarchar(100) NOT NULL,
    [TablonNombre] nvarchar(100) NOT NULL,
    [TipoCosecha] int NOT NULL,
    [OperadorId] int NOT NULL,
    [OperadorNombre] nvarchar(150) NOT NULL,
    [OperadorNucleoCodigo] nvarchar(30) NOT NULL,
    [TractoristaId] int NOT NULL,
    [TractoristaNombre] nvarchar(150) NOT NULL,
    [TractoristaNucleoCodigo] nvarchar(30) NOT NULL,
    [ChoferId] int NOT NULL,
    [ChoferNombre] nvarchar(150) NOT NULL,
    [VehiculoId] int NOT NULL,
    [VehiculoPlaca] nvarchar(20) NOT NULL,
    [RemeseroId] int NOT NULL,
    [RemeseroNombre] nvarchar(150) NOT NULL,
    [NucleoCorteCodigo] nvarchar(30) NOT NULL,
    [NucleoAlzaEmpujeCodigo] nvarchar(30) NOT NULL,
    [NucleoTransporteCodigo] nvarchar(30) NOT NULL,
    [InicioCarga] datetime2 NOT NULL,
    [FinCarga] datetime2 NOT NULL,
    [LlegadaCentral] datetime2 NULL,
    [PesoBrutoT] decimal(18,2) NULL,
    [TaraT] decimal(18,2) NULL,
    [Estado] int NOT NULL,
    [MotivoAnulacion] nvarchar(500) NULL,
    [FechaConfirmacion] datetime2 NULL,
    [FechaAnulacion] datetime2 NULL,
    [CreadoPorId] int NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    [FacturaClienteId] int NULL,
    CONSTRAINT [PK_Remesas] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [SalidasInventario] (
    [Id] int NOT NULL IDENTITY,
    [Fecha] datetime2 NOT NULL,
    [ArticuloCodigo] nvarchar(30) NOT NULL,
    [ArticuloNombre] nvarchar(150) NOT NULL,
    [Unidad] nvarchar(20) NOT NULL,
    [Cantidad] decimal(18,2) NOT NULL,
    [CostoUnitario] decimal(18,2) NOT NULL,
    [ActivoId] int NULL,
    [ActivoEtiqueta] nvarchar(150) NOT NULL,
    [MantenimientoId] int NULL,
    [Motivo] nvarchar(500) NOT NULL,
    [Estado] int NOT NULL,
    [MotivoAnulacion] nvarchar(500) NULL,
    [StockForzado] bit NOT NULL,
    [FechaConfirmacion] datetime2 NULL,
    [FechaAnulacion] datetime2 NULL,
    [CreadoPorId] int NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    CONSTRAINT [PK_SalidasInventario] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [ValesCombustible] (
    [Id] int NOT NULL IDENTITY,
    [Fecha] datetime2 NOT NULL,
    [TanqueId] int NOT NULL,
    [TanqueNombre] nvarchar(150) NOT NULL,
    [ActivoId] int NOT NULL,
    [ActivoCodigo] nvarchar(30) NOT NULL,
    [ActivoEtiqueta] nvarchar(150) NOT NULL,
    [EsTransporte] bit NOT NULL,
    [Litros] decimal(18,2) NOT NULL,
    [Lectura] decimal(18,2) NULL,
    [ResponsableNombre] nvarchar(150) NOT NULL,
    [ConsumoPorUnidad] decimal(18,2) NULL,
    [PromedioHistorico] decimal(18,2) NULL,
    [AlertaConsumo] bit NOT NULL,
    [Estado] int NOT NULL,
    [MotivoAnulacion] nvarchar(500) NULL,
    [FechaConfirmacion] datetime2 NULL,
    [FechaAnulacion] datetime2 NULL,
    [CreadoPorId] int NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    [Notas] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_ValesCombustible] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819025551_Fase3_DocumentosPlanos', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [FacturasCliente] (
    [Id] int NOT NULL IDENTITY,
    [ClienteNombre] nvarchar(150) NOT NULL,
    [DiasCredito] int NOT NULL,
    [Estado] int NOT NULL,
    [MotivoAnulacion] nvarchar(500) NULL,
    [FechaEmision] datetime2 NULL,
    [FechaVencimiento] datetime2 NULL,
    [FechaCobro] datetime2 NULL,
    [FechaAnulacion] datetime2 NULL,
    [CreadoPorId] int NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    CONSTRAINT [PK_FacturasCliente] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Fincas] (
    [Id] int NOT NULL IDENTITY,
    [CodigoCam] nvarchar(30) NOT NULL,
    [Nombre] nvarchar(150) NOT NULL,
    CONSTRAINT [PK_Fincas] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Liquidaciones] (
    [Id] int NOT NULL IDENTITY,
    [SujetoTipo] int NOT NULL,
    [SujetoCodigo] nvarchar(30) NOT NULL,
    [SujetoNombre] nvarchar(150) NOT NULL,
    [PeriodoDesde] datetime2 NOT NULL,
    [PeriodoHasta] datetime2 NOT NULL,
    [RemesaIdsIncluidas] nvarchar(max) NOT NULL,
    [Estado] int NOT NULL,
    [MotivoAnulacion] nvarchar(500) NULL,
    [FechaCierre] datetime2 NULL,
    [FechaPago] datetime2 NULL,
    [FechaAnulacion] datetime2 NULL,
    [CreadoPorId] int NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    CONSTRAINT [PK_Liquidaciones] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [FacturaClienteLinea] (
    [Id] int NOT NULL IDENTITY,
    [RemesaId] int NOT NULL,
    [FincaNombre] nvarchar(150) NOT NULL,
    [FechaRecepcion] datetime2 NOT NULL,
    [Servicio] int NOT NULL,
    [NucleoCodigo] nvarchar(30) NOT NULL,
    [Toneladas] decimal(18,2) NOT NULL,
    [TarifaMonto] decimal(18,2) NOT NULL,
    [FacturaClienteId] int NOT NULL,
    CONSTRAINT [PK_FacturaClienteLinea] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FacturaClienteLinea_FacturasCliente_FacturaClienteId] FOREIGN KEY ([FacturaClienteId]) REFERENCES [FacturasCliente] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Lote] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [FincaId] int NOT NULL,
    CONSTRAINT [PK_Lote] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Lote_Fincas_FincaId] FOREIGN KEY ([FincaId]) REFERENCES [Fincas] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [LiquidacionLinea] (
    [Id] int NOT NULL IDENTITY,
    [Concepto] nvarchar(150) NOT NULL,
    [Origen] int NOT NULL,
    [Cantidad] decimal(18,2) NOT NULL,
    [UnidadTexto] nvarchar(20) NOT NULL,
    [TarifaMonto] decimal(18,2) NULL,
    [Monto] decimal(18,2) NOT NULL,
    [EsDeduccion] bit NOT NULL,
    [LiquidacionId] int NOT NULL,
    CONSTRAINT [PK_LiquidacionLinea] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LiquidacionLinea_Liquidaciones_LiquidacionId] FOREIGN KEY ([LiquidacionId]) REFERENCES [Liquidaciones] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Tablon] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [LoteId] int NOT NULL,
    CONSTRAINT [PK_Tablon] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tablon_Lote_LoteId] FOREIGN KEY ([LoteId]) REFERENCES [Lote] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_FacturaClienteLinea_FacturaClienteId] ON [FacturaClienteLinea] ([FacturaClienteId]);
GO

CREATE INDEX [IX_LiquidacionLinea_LiquidacionId] ON [LiquidacionLinea] ([LiquidacionId]);
GO

CREATE INDEX [IX_Lote_FincaId] ON [Lote] ([FincaId]);
GO

CREATE INDEX [IX_Tablon_LoteId] ON [Tablon] ([LoteId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819025744_Fase4_ColeccionesAnidadas', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [EventosOperacion] (
    [Id] int NOT NULL IDENTITY,
    [RemesaId] int NOT NULL,
    [Tipo] int NOT NULL,
    [FechaHora] datetime2 NOT NULL,
    [Descripcion] nvarchar(500) NOT NULL,
    [Autor] nvarchar(150) NOT NULL,
    CONSTRAINT [PK_EventosOperacion] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819025852_Fase5_EventoOperacion', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inventarios]') AND [c].[name] = N'StockActual');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Inventarios] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Inventarios] ALTER COLUMN [StockActual] decimal(18,2) NOT NULL;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inventarios]') AND [c].[name] = N'StockMinimo');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Inventarios] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Inventarios] ALTER COLUMN [StockMinimo] decimal(18,2) NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260819032146_FixStockActualStockMinimoDecimal', N'8.0.28');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [ValesCombustible] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [Tarifas] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [TanquesCombustible] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [SalidasInventario] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [Remesas] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [ReglasMantenimiento] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [RecargasCombustible] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [Proveedores] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [PersonalCampo] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [Nucleos] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [MantenimientoRegistros] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [Liquidaciones] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [Jornadas] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [Inventarios] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [Fincas] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [FacturasProveedor] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [FacturasCliente] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [EventosOperacion] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [Empleados] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [ConceptosNomina] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [ActivosFlota] ADD [OrganizacionId] int NOT NULL DEFAULT 1;
GO

CREATE TABLE [Organizaciones] (
    [Id] int NOT NULL IDENTITY,
    [Codigo] nvarchar(20) NOT NULL,
    [Nombre] nvarchar(150) NOT NULL,
    [Activa] bit NOT NULL,
    CONSTRAINT [PK_Organizaciones] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PermisosUsuario] (
    [Id] int NOT NULL IDENTITY,
    [OrganizacionId] int NOT NULL,
    [UsuarioId] int NOT NULL,
    [UsuarioNombre] nvarchar(60) NOT NULL,
    [Permiso] nvarchar(80) NOT NULL,
    [Concedido] bit NOT NULL,
    CONSTRAINT [PK_PermisosUsuario] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PeticionesCambio] (
    [Id] int NOT NULL IDENTITY,
    [OrganizacionId] int NOT NULL,
    [Permiso] nvarchar(80) NOT NULL,
    [Accion] nvarchar(120) NOT NULL,
    [TipoEntidad] nvarchar(60) NOT NULL,
    [EntidadId] nvarchar(60) NOT NULL,
    [EntidadDescripcion] nvarchar(300) NOT NULL,
    [Motivo] nvarchar(500) NOT NULL,
    [Estado] int NOT NULL,
    [SolicitadoPorId] int NOT NULL,
    [SolicitadoPorNombre] nvarchar(150) NOT NULL,
    [SolicitadoEn] datetime2 NOT NULL,
    [ResueltoPorId] int NULL,
    [ResueltoPorNombre] nvarchar(150) NOT NULL,
    [ResueltoEn] datetime2 NULL,
    [ComentarioResolucion] nvarchar(500) NOT NULL,
    CONSTRAINT [PK_PeticionesCambio] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Usuarios] (
    [Id] int NOT NULL IDENTITY,
    [OrganizacionId] int NOT NULL,
    [NombreUsuario] nvarchar(60) NOT NULL,
    [NombreCompleto] nvarchar(150) NOT NULL,
    [Rol] int NOT NULL,
    [PasswordHash] nvarchar(120) NOT NULL,
    [PasswordSalt] nvarchar(60) NOT NULL,
    [Activo] bit NOT NULL,
    CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
);
GO

CREATE UNIQUE INDEX [IX_Organizaciones_Codigo] ON [Organizaciones] ([Codigo]);
GO

CREATE UNIQUE INDEX [IX_PermisosUsuario_UsuarioId_Permiso] ON [PermisosUsuario] ([UsuarioId], [Permiso]);
GO

CREATE UNIQUE INDEX [IX_Usuarios_NombreUsuario] ON [Usuarios] ([NombreUsuario]);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activa', N'Codigo', N'Nombre') AND [object_id] = OBJECT_ID(N'[Organizaciones]'))
    SET IDENTITY_INSERT [Organizaciones] ON;
INSERT INTO [Organizaciones] ([Id], [Activa], [Codigo], [Nombre])
VALUES (1, CAST(1 AS bit), N'NUC-1', N'Núcleo inicial');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activa', N'Codigo', N'Nombre') AND [object_id] = OBJECT_ID(N'[Organizaciones]'))
    SET IDENTITY_INSERT [Organizaciones] OFF;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260820172038_Fase6_OrganizacionYSeguridad', N'8.0.28');
GO

COMMIT;
GO

