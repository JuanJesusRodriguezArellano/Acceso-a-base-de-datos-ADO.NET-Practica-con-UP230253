# Acceso a Base de Datos ADO.NET — Práctica UP230253

API REST para administrar productos usando **ADO.NET puro** con MySQL, sin ORM (sin Entity Framework). Desarrollada con .NET 10 Minimal API.

---

## Descripción del Flujo y Métodos

La aplicación sigue una arquitectura en capas donde cada parte tiene una responsabilidad clara:

### Conexión (`Data/ConexionDb.cs`)
La clase `ConexionDb` implementa la interfaz `IConexionDb` y se encarga de crear objetos `MySqlConnection` a partir de la cadena de conexión que viene de `appsettings.json`. No abre la conexión directamente — la entrega cerrada para que cada método del repositorio la abra y cierre con `using`, aprovechando el connection pooling que ADO.NET maneja internamente. El método `ProbarConexion()` abre y cierra una conexión de prueba para verificar que MySQL está respondiendo.

### Modelo (`Models/Producto.cs`)
La clase `Producto` mapea una fila de la tabla `productos` en MySQL. Cada propiedad corresponde a una columna: `Id` (auto-increment), `Nombre`, `Descripcion`, `Categoria`, `Precio` (decimal para evitar errores de redondeo con dinero), `Stock`, `Activo` (para dar de baja sin borrar) y `FechaRegistro` (la pone MySQL automáticamente).

### Repositorio (`Repositories/ProductoRepository.cs`)
Es la clase central del proyecto. Implementa las interfaces `IProductoRepository` e `IProductoBusqueda` y es la **única clase que escribe SQL**. Todos los métodos siguen la misma receta de ADO.NET:

1. Pedir una `MySqlConnection` a `IConexionDb` y abrirla con `.Open()`
2. Crear un `MySqlCommand` con la sentencia SQL correspondiente
3. Agregar los valores como **parámetros** (`@nombre`, `@id`, etc.) con `AddWithValue()` — nunca se concatenan valores del usuario dentro del SQL, lo que previene inyección SQL
4. Ejecutar con el método apropiado:
   - **`ExecuteReader()`** para SELECT → devuelve filas que se recorren con `while (lector.Read())`
   - **`ExecuteScalar()`** para obtener un solo valor → el ID generado tras un INSERT
   - **`ExecuteNonQuery()`** para UPDATE/DELETE → devuelve cuántas filas fueron afectadas
5. Todo se libera automáticamente con `using` al salir del método

**Métodos CRUD implementados:**

| Método | Operación SQL | Qué hace |
|--------|--------------|----------|
| `ObtenerTodos()` | `SELECT` | Devuelve todos los productos ordenados por ID |
| `ObtenerPorId(id)` | `SELECT WHERE id = @id` | Busca un producto por su llave primaria, devuelve null si no existe |
| `Insertar(producto)` | `INSERT` + `SELECT LAST_INSERT_ID()` | Crea un registro y devuelve el ID que MySQL generó |
| `Actualizar(producto)` | `UPDATE SET ... WHERE id = @id` | Modifica los datos, devuelve true/false según si existía |
| `Eliminar(id)` | `DELETE WHERE id = @id` | Borra el registro, devuelve true/false según si existía |
| `Buscar(filtros)` | `SELECT` dinámico con `WHERE 1=1 AND ...` | Filtra por nombre (parcial), categoría, rango de precio o estado activo |

### Endpoints (`Endpoints/ProductosEndpoints.cs`)
Mapea cada ruta HTTP a un método del repositorio:

| Ruta | HTTP | Operación | Respuestas |
|------|------|-----------|------------|
| `GET /productos` | GET | Listar todos | 200 |
| `GET /productos/{id}` | GET | Obtener por ID | 200 / 404 |
| `GET /productos/buscar` | GET | Búsqueda con filtros | 200 / 400 |
| `POST /productos` | POST | Crear nuevo | 201 / 400 |
| `PUT /productos/{id}` | PUT | Actualizar existente | 204 / 400 / 404 |
| `DELETE /productos/{id}` | DELETE | Eliminar | 204 / 404 |

### Inyección de Dependencias (`Program.cs`)
Se registran en el contenedor de .NET:
- `IConexionDb` → `ConexionDb` como **Singleton** (solo guarda la cadena de conexión)
- `IProductoRepository` e `IProductoBusqueda` → `ProductoRepository` como **Scoped** (uno nuevo por petición HTTP)

Así los endpoints dependen de interfaces, no de clases concretas, y se podría cambiar MySQL por otro motor sin tocar los endpoints.

### Inicialización Automática (`Data/InicializadorDb.cs`)
Al arrancar, crea la base de datos `productos_db`, la tabla `productos` y 5 registros de ejemplo si la tabla está vacía. Si MySQL no está disponible, la API arranca igual pero avisa en consola.

---

## Interface Implementada

```csharp
public interface IProductoRepository
{
    List<Producto> ObtenerTodos();
    Producto? ObtenerPorId(int id);
    int Insertar(Producto producto);
    bool Actualizar(Producto producto);
    bool Eliminar(int id);
}
```

---

## Estructura del Proyecto

```
ProductosAPI/
├── Program.cs                       ← Arranque, DI, Swagger, rutas
├── Models/
│   └── Producto.cs                  ← Modelo de datos
├── Data/
│   ├── IConexionDb.cs               ← Interfaz de conexión
│   ├── ConexionDb.cs                ← Implementación con MySqlConnection
│   └── InicializadorDb.cs           ← Creación automática de BD y tabla
├── Repositories/
│   ├── IProductoRepository.cs       ← Interfaz CRUD (la de la actividad)
│   ├── IProductoBusqueda.cs         ← Interfaz de búsqueda
│   └── ProductoRepository.cs        ← Implementación ADO.NET de todo el SQL
├── Endpoints/
│   ├── ProductosEndpoints.cs        ← Rutas HTTP /productos
│   └── ConexionEndpoints.cs         ← Ruta /conexion
├── Database/
│   └── productos.sql                ← Script SQL alternativo
├── appsettings.json                 ← Cadena de conexión MySQL
├── compose.yaml / Dockerfile        ← Docker (API + MySQL)
└── ProductosAPI.csproj              ← .NET 10 + paquetes NuGet
```

## Requisitos

- .NET 10 SDK
- MySQL 8+ (local o con Docker)

## Ejecutar

```bash
# Con MySQL local
dotnet run

# Con Docker Compose (incluye MySQL)
docker compose up --build
```

Abrir: **http://localhost:5135/swagger**

---

**Materia:** ISC09C — Acceso a Base de Datos  
**Alumno:** UP230253
