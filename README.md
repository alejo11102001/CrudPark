# 🅿️ CrudPark

Sistema de gestión de parqueadero desarrollado en **C# .NET** con control de tickets, tarifas, mensualidades, operadores y notificaciones. Dockerizado para fácil despliegue.

---

## ⚙️ Tech Stack

- **C# .NET** — ASP.NET Core Web API
- **Entity Framework Core** — ORM
- **SQL Server** — Base de datos
- **Docker** — Contenedorización

---

## 📋 Módulos

| Módulo | Descripción |
|---|---|
| **Tickets** | Registro de entrada y salida de vehículos |
| **Tarifas** | Configuración de precios por tipo de vehículo |
| **Mensualidades** | Gestión de clientes con pago mensual |
| **Operadores** | Administración de personal |
| **Notificaciones** | Alertas y avisos del sistema |
| **Dashboard** | Resumen de actividad y estadísticas |

---

## 🚀 Cómo correrlo localmente

### Con Docker
```bash
git clone https://github.com/alejo11102001/CrudPark.git
cd CrudPark
docker build -t crudpark .
docker run -p 5000:80 crudpark
```

### Sin Docker
```bash
cd CrudPark
dotnet restore
dotnet ef database update
dotnet run
```

API disponible en `https://localhost:7001`
Swagger en `https://localhost:7001/swagger`

---

## 👤 Autor

**Diego Alejandro Zuluaga Yepes**
- GitHub: [@alejo11102001](https://github.com/alejo11102001)
- LinkedIn: [diego-zuluaga-yepes](https://linkedin.com/in/diego-zuluaga-yepes-239137272)
- Portfolio: [alejo11102001.github.io/Portafolio](https://alejo11102001.github.io/Portafolio)
