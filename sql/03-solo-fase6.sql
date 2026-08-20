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

