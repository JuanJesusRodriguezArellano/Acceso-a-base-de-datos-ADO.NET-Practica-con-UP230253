# Acceso a Base de Datos con ADO.NET — Juan Jesus Rodriguez Arellano (UP230253)

Hice esta API para la práctica de la materia. Básicamente es un CRUD de productos que se conecta a MySQL usando **ADO.NET puro**, o sea, sin Entity Framework ni nada de eso. Todo el SQL lo escribo yo directamente con `MySqlCommand` y parámetros.

La idea era demostrar cómo funciona la conexión a base de datos, las consultas, inserts, updates y deletes usando el patrón repositorio con inyección de dependencias.

---

## Cómo está organizado el proyecto

Lo dividí en capas para que cada parte haga una sola cosa:

### La conexión — `Data/ConexionDb.cs`

Aquí es donde se crea la conexión a MySQL. La clase `ConexionDb` lee la cadena de conexión del archivo `appsettings.json` y cuando alguien necesita hablar con la base de datos, le da un objeto `MySqlConnection` listo para usarse. No la abre ella, sino que el que la pide la abre con `.Open()` y cuando termina se cierra sola gracias al `using`. Esto funciona bien porque ADO.NET tiene un pool de conexiones interno, entonces abrir y cerrar no es lento, en realidad se reutilizan.

También tiene un método `ProbarConexion()` que sirve para checar si MySQL está prendido y respondiendo — es lo que usa el endpoint `GET /conexion`.

### El modelo — `Models/Producto.cs`

Es la clase que representa un producto. Tiene las propiedades `Id`, `Nombre`, `Descripcion`, `Categoria`, `Precio` (lo puse como decimal porque con double se pierden centavos), `Stock`, `Activo` (para poder desactivar productos sin borrarlos) y `FechaRegistro` que la pone MySQL solo con `CURRENT_TIMESTAMP`.

### El repositorio — `Repositories/ProductoRepository.cs`

Esta es la clase más importante del proyecto. Implementa `IProductoRepository` y `IProductoBusqueda`, y es la **única parte que toca SQL**. Los endpoints nunca ven una sentencia SQL.

Todos los métodos hacen más o menos lo mismo:

1. Le pido una conexión a `IConexionDb` y la abro
2. Armo un `MySqlCommand` con el SQL que necesito
3. Los valores los paso como **parámetros** (`@nombre`, `@precio`, etc.) para que no haya riesgo de inyección SQL — nunca concateno lo que manda el usuario dentro del string del SQL
4. Ejecuto con el método que toque:
   - `ExecuteReader()` cuando necesito leer filas (los SELECT)
   - `ExecuteScalar()` cuando solo quiero un valor (como el ID nuevo después de un INSERT)
   - `ExecuteNonQuery()` cuando solo me importa cuántas filas se afectaron (UPDATE y DELETE)
5. El `using` se encarga de cerrar todo al salir

Los métodos que tiene son:

- **`ObtenerTodos()`** — hace un `SELECT` de toda la tabla y regresa una lista
- **`ObtenerPorId(id)`** — busca un producto por su ID, si no existe regresa `null`
- **`Insertar(producto)`** — mete un registro nuevo y con `LAST_INSERT_ID()` saca el ID que MySQL le asignó
- **`Actualizar(producto)`** — hace un `UPDATE` y si no encuentra el ID regresa `false`
- **`Eliminar(id)`** — hace un `DELETE`, igual regresa `true` o `false` según si existía
- **`Buscar(filtros)`** — arma un `SELECT` dinámico dependiendo de qué filtros le llegaron (nombre, categoría, precio mín/máx, activo). Usa el truco de `WHERE 1=1` para ir pegando `AND` sin preocuparse de cuál es el primero

### Los endpoints — `Endpoints/ProductosEndpoints.cs`

Aquí se definen las rutas de la API. Cada endpoint recibe la petición, valida lo básico (que el nombre no esté vacío, que el precio no sea negativo, etc.) y llama al repositorio. Después traduce el resultado a un código HTTP:

| Ruta | Verbo | Qué hace | Respuestas |
|------|-------|----------|------------|
| `GET /productos` | GET | Trae todos los productos | 200 |
| `GET /productos/{id}` | GET | Trae uno por su ID | 200 o 404 |
| `GET /productos/buscar` | GET | Busca con filtros opcionales | 200 o 400 |
| `POST /productos` | POST | Crea un producto nuevo | 201 o 400 |
| `PUT /productos/{id}` | PUT | Actualiza un producto | 204, 400 o 404 |
| `DELETE /productos/{id}` | DELETE | Borra un producto | 204 o 404 |

### La inyección de dependencias — `Program.cs`

En el archivo principal registro los servicios para que .NET sepa qué clase dar cuando alguien pida una interfaz:
- `IConexionDb` → `ConexionDb` como **Singleton** (porque solo guarda un string, la cadena de conexión)
- `IProductoRepository` y `IProductoBusqueda` → `ProductoRepository` como **Scoped** (uno nuevo por cada petición HTTP)

La ventaja de esto es que los endpoints no conocen la clase concreta, solo la interfaz. Si un día quiero cambiar MySQL por SQL Server, solo cambio el repositorio y la conexión, los endpoints ni se enteran.

### La inicialización — `Data/InicializadorDb.cs`

Cuando la API arranca, esta clase se conecta a MySQL y crea la base de datos `productos_db` y la tabla `productos` si no existen. Si la tabla está vacía, mete 5 productos de ejemplo para que haya datos con qué probar. Si MySQL no está prendido, la API arranca igual pero avisa en la consola.

---

## La interfaz que se pidió

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

Está implementada tal cual en `ProductoRepository`. Para la búsqueda agregué otra interfaz aparte (`IProductoBusqueda`) en vez de modificar la original.

---

## Estructura de archivos

```
ProductosAPI/
├── Program.cs                       ← Donde arranca todo
├── Models/
│   └── Producto.cs                  ← La clase del producto
├── Data/
│   ├── IConexionDb.cs               ← Interfaz de la conexión
│   ├── ConexionDb.cs                ← Se conecta a MySQL
│   └── InicializadorDb.cs           ← Crea la BD y tabla al iniciar
├── Repositories/
│   ├── IProductoRepository.cs       ← La interfaz de la actividad
│   ├── IProductoBusqueda.cs         ← Interfaz extra para buscar
│   └── ProductoRepository.cs        ← Aquí está todo el SQL
├── Endpoints/
│   ├── ProductosEndpoints.cs        ← Las rutas de /productos
│   └── ConexionEndpoints.cs         ← La ruta /conexion
├── Database/
│   └── productos.sql                ← Script por si quieres crear la BD a mano
├── appsettings.json                 ← Cadena de conexión a MySQL
├── compose.yaml / Dockerfile        ← Para correrlo con Docker
└── ProductosAPI.csproj              ← Proyecto .NET 10
```

## Cómo correrlo

Necesitas .NET 10 SDK y MySQL 8 (o Docker).

```bash
# Si ya tienes MySQL instalado
dotnet run

# Si prefieres Docker (levanta MySQL y la API juntos)
docker compose up --build
```

Después abres **http://localhost:5135/swagger** y desde ahí puedes probar todos los endpoints.

---

**Materia:** Móvil ISC09C  
**Profesor:** Marco Ulises Padilla Galván  
**Alumno:** Juan Jesus Rodriguez Arellano — UP230253
