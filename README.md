# E-commerce ICS 2026 - Comision 4K3

Integrantes: 
Fernandez, Jose Agustin - Iturre, Horacio Martin - Quintero, Paulina Rocio - Vides Gimenez, Ramiro 

⚙️ Instrucciones para configurar y ejecutar el proyecto ⚙️

🔧 Requisitos

- [.NET SDK 8.0+](https://dotnet.microsoft.com/en-us/download)
- SQL Server o SQL Server Express
- Visual Studio 2022 o Visual Studio Code

📦 Clonar el Repositorio

```bash
git clone https://github.com/UTN-ICS2026-4K3-58536-58584-48186-57257/ecommerce-ics2026-backend
cd ecommerce-ics2026-backend
```

⚙️ Configuración de Base de Datos

1. Asegurese de tener una instancia de SQL Server corriendo.
2. Modifique el archivo `appsettings.json` con tu string de conexión:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=Dsw2025Tpi;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

🛠️ Migraciones y Seed de Datos

Desde la Consola del Administrador de Paquetes de Visual Studio:

Para Dsw2025TpiContext:

```powershell
PM> Add-Migration Nombre_Migracion -Context Dsw2025TpiContext
PM> Update-Database -Context Dsw2025TpiContext
```

Esto generará las migraciones en el proyecto de datos y actualizará la base de datos usando el contexto `Dsw2025TpiContext`.

🚀 Ejecutar el Proyecto

Desde Visual Studio:
1. Establezca `Dsw2025Tpi.Api` como proyecto de inicio.
2. Presione `Ctrl + F5` para ejecutar sin depurar, o `F5` para iniciar con depuración.

O desde la terminal:

```bash
dotnet run --project Dsw2025Tpi.Api
```

---

🔐 Autenticación

Usuarios de prueba:

#### Cliente
- **Usuario:** `customer`
- **Contraseña:** `Customer123*`

#### Administrador
- **Usuario:** `admin`
- **Contraseña:** `Admin123*`

Endpoint de Login:

```````````````
POST /api/login
```````````````

**Ejemplo de JSON:**
```json
{
  "username": "customer",
  "password": "Customer123*"
}
```

**Respuesta esperada:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR..."
}
```

Use este token en "Authorize":

```
Bearer  (http, Bearer)
Ingrese un token JWT válido. Ejemplo: Bearer {token}

Value:
**Ingrese el token aqui**
```

---

📌 Descripción de los endpoints implementados y cómo utilizarlos. 📌

-PRODUCTOS:

| Método | Ruta               | Descripción                                                                      |
| ------ | ------------------ | -------------------------------------------------------------------------------- |
| POST   | /api/products      | Crear un nuevo producto (Admin)                                                  |
| GET    | /api/products      | Obtener todos los productos (No se necesita autenticacion)                       |
| GET    | /api/products/{id} | Ver detalles de un producto especifico (No se necesita autenticacion)            |
| PUT    | /api/products/{id} | Modificar un producto existente (Admin)                                          | 
| PATCH  | /api/products/{id} | Inhabilitar un producto (Admin)                                                  |

-ORDENES:

| Método | Ruta                    | Descripción                                                                 |
| ------ | ----------------------- | --------------------------------------------------------------------------- |
| POST   | /api/orders             | Crear una orden de compra (Customer)                                        |
| GET    | /api/orders             | Listar órdenes filtradas por estado, cliente o paginacion (Customer, Admin) |
| GET    | /api/orders/{id}        | Ver detalle de una orden especifica (Customer, Admin)                       |
| PUT    | /api/orders/{id}/status | Actualizar estado de una orden (Admin)                                      |


---------------------------- PRODUCTOS ----------------------------

🔹 Crear producto (Admin)

``````````````````
POST /api/products
``````````````````

```json de ejemplo
{
  "sku": "ABC123",
  "internalCode": "INT-001",
  "name": "Producto de ejemplo",
  "description": "Descripción del producto.",
  "currentUnitPrice": 199.99,
  "stockQuantity": 50
}
```

---

🔹 Obtener todos los productos (No se necesita autenticacion)

``````````````````
GET /api/products
``````````````````

Sin body, presione ejecutar para obtener todos los productos.

---

🔹 Obtener Producto por ID (No se necesita autenticacion)

``````````````````````
GET /api/products/{id}
``````````````````````

Reemplace `{id}` por el ID del producto.

---

🔹 Actualizar un producto (Admin)

``````````````````````
PUT /api/products/{id}
``````````````````````

Reemplace `{id}` por el ID del producto.

```json de ejemplo
{
  "sku": "ABC123",
  "internalCode": "INT-001",
  "name": "Producto actualizado",
  "description": "Nueva descripción.",
  "currentUnitPrice": 299.99,
  "stockQuantity": 75
}
```

---

🔹 Inhabilitar Producto (Admin)

````````````````````````
PATCH /api/products/{id}
````````````````````````

Reemplace `{id}` por el ID del producto.


---------------------------- ORDENES ----------------------------

 🔹 Crear Orden (Customer)

````````````````
POST /api/orders
````````````````

```json de ejemplo
{
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "shippingAddress": "Calle Falsa 123",
  "billingAddress": "Calle Falsa 123",
  "orderItems": [
    {
      "productId": "9318924F-92DC-445E-9D5E-19EC67AFC244",
      "quantity": 2
    }
  ]
}
```

---

🔹 Obtener Todas las Órdenes (Customer, Admin)

```````````````
GET /api/orders
```````````````

Opcional: usar parámetros `status`, `customerId`, `pageNumber`, `pageSize`.

---

🔹 Obtener Orden por ID (Customer, Admin)

````````````````````
GET /api/orders/{id}
````````````````````

Reemplace `{id}` por el ID de la orden.

---

🔹 Actualizar Estado de Orden (Admin)

```````````````````````````
PUT /api/orders/{id}/status
```````````````````````````

```json de ejemplo
{
  "newStatus": "Processing"
}
```

**Estados disponibles:**

* `Pending`
* `Processing`
* `Shipped`
* `Delivered`
* `Cancelled`

**Condiciones de cambio:**

* Solo es posible avanzar o cancelar una orden.
* No se puede volver a un estado anterior.
* `Cancelled` solo puede aplicarse desde `Pending` o `Processing`.
* No se puede modificar una orden que ya está en `Delivered` o `Cancelled`.

---

## 📫 Contacto

Para dudas o soporte, contacte a los integrantes del grupo.


## 🐳 Ejecutar con Docker

**Requisitos:** Docker Engine y Docker Compose.

| Variable | Descripción |
|---|---|
| `ASPNETCORE_ENVIRONMENT` | Entorno de ejecución: `Development` o `Production` |
| `ConnectionStrings__DefaultConnection` | Cadena de conexión a SQL Server |
| `Jwt__Key` | Clave para firmar los tokens JWT (mínimo 32 caracteres) |

**Levantar el entorno local:**

1. Copiar `.env.example` como `.env` y completar los valores.
2. Desde la raíz del repositorio, ejecutar: `docker compose up --build -d`
3. Verificar en `http://localhost:8080/healthcheck` y `http://localhost:8080/swagger`
4. Para detener: `docker compose down`

La base de datos se crea automáticamente al iniciar la API.
