# Software ASO

Sistema de gestión para centros de procesamiento de caña de azúcar (zafra).

## Requisitos

- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10/11

## Ejecutar

```bash
cd ASO.Desktop
dotnet run
```

O abrir `ASO.slnx` en Visual Studio 2022.

## Estructura actual

```
ASO/
├── ASO.slnx
└── ASO.Desktop/          # Shell WPF — interfaz base
    ├── Controls/         # Sidebar y componentes reutilizables
    ├── Styles/           # Paleta de colores y estilos
    └── Views/            # Vistas por módulo (Dashboard de Producción)
```

## Estado del proyecto

**Fase actual:** fundación visual (shell + dashboard placeholder).

Incluido:
- Sidebar con módulos: Producción, Inventario, Calidad, Mantenimiento
- Menú superior (Archivo, Ver, Herramientas, Ayuda)
- Área de contenido vacía (dashboard placeholder)

Pendiente (según plan de acción):
- Capas Application, Domain, Infrastructure
- Entity Framework Core + SQL Server
- Autenticación, maestros y reglas de negocio
