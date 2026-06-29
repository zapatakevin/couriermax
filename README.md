# CourierMax API — Prueba Técnica .NET (Senior)

API REST en **.NET 8 + ASP.NET Core** para la gestión del ciclo de vida de envíos
de CourierMax. Implementa los 6 requerimientos funcionales y las 5 reglas de
negocio del enunciado, con pruebas automatizadas y principios SOLID / DRY / KISS
aplicados de forma explícita.

---

## 1. Resumen de la solución

| Capa | Proyecto | Responsabilidad |
|------|----------|------------------|
| **Domain** | `CourierMax.Domain` | Entidades, value objects, enums, excepciones de dominio. **Cero dependencias externas.** |
| **Application** | `CourierMax.Application` | Servicios (casos de uso), DTOs, validadores, contratos (interfaces) de repositorios. |
| **Infrastructure** | `CourierMax.Infrastructure` | EF Core (SQLite), `DbContext`, repositorios, seed de datos de referencia, DI. |
| **Api** | `CourierMax.Api` | Controllers REST, middleware de errores, Swagger, logging (Serilog), health check. |
| **Tests** | `CourierMax.Tests` | xUnit + FluentAssertions + WebApplicationFactory. Cubre dominio, servicios y API. |

Persistencia: **SQLite** (EF Core 8). Para empezar a trabajar basta con
`dotnet run` — la base se crea y se siembra sola en el primer arranque.

---

## 2. Requisitos previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (probado con `8.0.10`)
- macOS, Linux o Windows
- (Opcional) `curl`, [jq](https://stedolan.github.io/jq/), Postman

---

## 3. Cómo ejecutar

```bash
# 1) Restaurar y compilar
dotnet restore
dotnet build

# 2) Arrancar la API (puerto 5080 por defecto, Swagger en /swagger)
dotnet run --project src/CourierMax.Api

# 3) Ejecutar tests
dotnet test

# Alternativas con scripts:
./scripts/run.sh     # arranca la API
./scripts/test.sh    # corre tests con cobertura
./scripts/reset-db.sh # borra couriermax.db
```

Al primer arranque la API:

1. Crea `couriermax.db` (SQLite) en la raíz del proyecto Api.
2. Siembra las 4 ciudades, las 6 distancias con sus tarifas, los 3 vehículos
   y los 3 conductores del enunciado.

---

## 4. Endpoints principales

Base URL: `http://localhost:5080`

| Verbo | Ruta | Descripción | Códigos |
|-------|------|-------------|---------|
| GET | `/health` | Health check | 200 |
| GET | `/swagger` | Documentación interactiva (OpenAPI) | 200 |
| GET | `/api/v1/Reference/cities` | Ciudades válidas | 200 |
| GET | `/api/v1/Reference/distances` | Distancias y tarifas | 200 |
| GET | `/api/v1/Reference/vehicles` | Vehículos y conductores | 200 |
| GET | `/api/v1/Reference/drivers` | Conductores y vehículos | 200 |
| POST | `/api/v1/Shipments/quote` | Cotiza sin persistir | 200, 400, 409 |
| POST | `/api/v1/Shipments` | Crea un envío (RF-01) | 201, 400, 409 |
| GET | `/api/v1/Shipments/{id}` | Detalle de envío | 200, 404 |
| GET | `/api/v1/Shipments/by-code/{code}` | Búsqueda por tracking | 200, 404 |
| GET | `/api/v1/Shipments?page=1&pageSize=20&status=active` | Listado paginado | 200 |
| POST | `/api/v1/Shipments/{id}/assign` | Asigna a vehículo manual | 200, 404, 409 |
| POST | `/api/v1/Shipments/{id}/auto-assign` | Auto-asignación balanceada (RN-01) | 200, 404, 409 |
| POST | `/api/v1/Shipments/{id}/start-transit` | ASIGNADO → EN_TRANSITO | 200, 409 |
| POST | `/api/v1/Shipments/{id}/deliver` | EN_TRANSITO → ENTREGADO | 200, 409 |
| POST | `/api/v1/Shipments/{id}/cancel` | Cancelación con motivo (RN-03) | 200, 400, 409 |
| GET | `/api/v1/reports/overdue?from=...&to=...` | Envíos atrasados (RF-05) | 200 |
| GET | `/api/v1/reports/drivers/{id}/metrics` | Métricas de eficiencia (RF-06) | 200, 404 |

### 4.1 Ejemplos rápidos (curl)

```bash
# Health
curl -s http://localhost:5080/health

# Crear envío (caso del enunciado: Bogotá→Medellín, frágil, express, 5kg)
curl -s -X POST http://localhost:5080/api/v1/Shipments \
  -H 'Content-Type: application/json' \
  -d '{
    "sender":   {"name":"Juan Pérez","phone":"3105551234","address":"Calle 100 #15-20"},
    "recipient":{"name":"Ana López", "phone":"6015554321","address":"Carrera 50 #30-10"},
    "weightKg": 5, "lengthCm": 20, "widthCm": 20, "heightCm": 20,
    "packageType": 2, "serviceType": 1,
    "originCity": "Bogotá", "destinationCity": "Medellín"
  }'
# → 201 Created, totalFee = 40950

# Auto-asignar
curl -s -X POST http://localhost:5080/api/v1/Shipments/1/auto-assign

# Iniciar tránsito y entregar
curl -s -X POST http://localhost:5080/api/v1/Shipments/1/start-transit \
     -H 'Content-Type: application/json' -d '{"actorId":"driver-1"}'
curl -s -X POST http://localhost:5080/api/v1/Shipments/1/deliver \
     -H 'Content-Type: application/json' -d '{"actorId":"driver-1"}'

# Métricas del conductor 1
curl -s http://localhost:5080/api/v1/reports/drivers/1/metrics
```

Colección lista para Postman en [`docs/couriermax.postman_collection.json`](docs/couriermax.postman_collection.json).

---

## 5. Mapeo con los requerimientos del enunciado

| Requerimiento | Implementación |
|---------------|----------------|
| **RF-01** Creación de envío con código `CM-XXXXXXXX` | `ShipmentService.CreateAsync` + `TrackingCode.Generate` (con reintento en caso de colisión) |
| **RF-02** Estados `CREADO → ASIGNADO → EN_TRANSITO → ENTREGADO` y `CANCELADO` | `Shipment.AssignTo / StartTransit / MarkDelivered / Cancel` con `StateTransition` registrado |
| **RF-03** Asignación con validación de capacidad | `ShipmentService.AssignAsync` + `EnsureCapacity` |
| **RF-04** Tarifa: base + extra-kg + distancia + recargo | `TariffCalculator` (puro, sin EF) |
| **RF-05** Alertas de entrega atrasada (SLA días hábiles) | `ShipmentService.ListOverdueAsync` + `ColombianBusinessDayCalculator` |
| **RF-06** Métricas por conductor | `ShipmentService.GetDriverMetricsAsync` |
| **RN-01** Capacidad + balanceo | `ShipmentService.AutoAssignAsync` ordena por carga actual |
| **RN-02** Días hábiles y festivos colombianos 2026 | `ColombianBusinessDayCalculator` con set de festivos |
| **RN-03** Cancelación con motivo ≥ 5 chars | `Shipment.Cancel` valida y libera vehículo/conductor |
| **RN-04** Validaciones (teléfono 10d 3|6, peso 0.1-100, dim 1-200, ciudades) | Value Objects `PhoneNumber`, `PackageDimensions`, `Address`, `City` |
| **RN-05** Código único | Índice único en `Shipment.TrackingCode` + verificación previa en `ExistsTrackingCodeAsync` |

---

## 6. Decisiones arquitectónicas

### 6.1 Estilo — Clean Architecture en 4 proyectos

Decidí **Clean Architecture** (no Vertical Slices) por dos razones concretas
del plazo y el enunciado:

1. **Los RFs están muy acoplados entre sí.** RF-02 es transverse a RF-03/04/05;
   poner cada feature en su propia carpeta habría duplicado la agregación de
   `Shipment` y el grafo de `StateTransition`.
2. **El enunciado exige “justificación arquitectónica” explícita.** Las capas
   Domain / Application / Infrastructure / Api permiten señalar con precisión
   dónde vive cada responsabilidad.

#### Principios SOLID aplicados

- **S (SRP):**
  - `Shipment` solo modela el agregado y su máquina de estados.
  - `TariffCalculator` solo calcula tarifas.
  - `ColombianBusinessDayCalculator` solo sabe de festivos.
  - `ExceptionHandlingMiddleware` solo traduce excepciones a HTTP.
- **O (OCP):** `IBusinessDayCalculator` puede reemplazarse por uno configurable
  (por ejemplo, por país) sin tocar al servicio.
- **L (LSP):** todas las entidades heredan de `Entity` con igualdad por
  identidad; los repositorios exponen contratos consistentes.
- **I (ISP):** los repositorios están separados por agregado (`IShipmentRepository`,
  `IVehicleRepository`, …) — no hay una “god interface”.
- **D (DIP):** `ShipmentService` depende de `IShipmentRepository`, `IUnitOfWork`,
  `ITariffCalculator`, `IClock`. La concreción vive en Infrastructure.

#### DRY / KISS

- **DRY:** validaciones de value objects centralizadas (teléfono, dimensiones,
  dirección, peso); una sola fuente de verdad para festivos.
- **KISS:** sin MediatR/CQRS pesado — el plazo de 3 días no justifica el
  boilerplate. Los casos de uso son métodos directos de `IShipmentService`.

### 6.2 Persistencia — EF Core + SQLite

- **SQLite** evita la fricción de un Docker/SqlServer local y mantiene
  persistencia real entre reinicios (el evaluador puede revisar `couriermax.db`).
- **Code First** con configuraciones separadas por entidad (no “magic strings”
  en `OnModelCreating`).
- **Índices únicos** sobre `TrackingCode`, `Plate`, `Identification` y nombre
  de ciudad para reforzar `RN-05`.
- **Migraciones** automáticas con `EnsureCreatedAsync` (apropiado para el
  alcance de la prueba; en un proyecto real se usarían migraciones versionadas).

### 6.3 Errores — middleware centralizado

Un único `ExceptionHandlingMiddleware` traduce las excepciones de dominio a
ProblemDetails con códigos HTTP coherentes:

| Excepción | HTTP |
|-----------|------|
| `ValidationException` | 400 |
| `NotFoundException`   | 404 |
| `BusinessRuleException` | 409 |
| (resto) | 500 |

Esto cumple el requerimiento del enunciado (“manejo centralizado de errores”)
y mantiene los controllers limpios.

### 6.4 Testing — pirámide aplicada

- **Unit tests de dominio** (`ShipmentStateMachineTests`, `ValueObjectsTests`):
  la máquina de estados se prueba sin EF ni web.
- **Unit tests de aplicación** (`ShipmentServiceTests`): con dobles in-memory
  para los repositorios; rápidos, deterministas (usa `IClock`).
- **Tests de integración / API** (`ShipmentsApiTests`): `WebApplicationFactory<Program>`
  arranca todo el pipeline (incluido SQLite `:memory:`) y valida los códigos
  HTTP del enunciado (200, 201, 400, 404, 409).

Cobertura objetivo: **todos los flujos de negocio** (crear, asignar,
auto-asignar, transitar, entregar, cancelar, atrasados, métricas).

### 6.5 Seguridad

- Validación de input en el borde (value objects + middleware de errores).
- Sin secretos en el repositorio (`appsettings.json` sin credenciales).
- CORS abierto para evaluación local (en producción se restringiría a un
  dominio específico).

---

## 7. Estructura del repositorio

```
couriermax/
├─ CourierMax.sln
├─ README.md
├─ .gitignore
├─ .editorconfig
├─ src/
│  ├─ CourierMax.Domain/         # Entidades, value objects, enums, excepciones
│  ├─ CourierMax.Application/    # Servicios, DTOs, mapeos, interfaces
│  ├─ CourierMax.Infrastructure/ # EF Core + SQLite + seed + DI
│  └─ CourierMax.Api/            # Controllers, middleware, Swagger
├─ tests/
│  └─ CourierMax.Tests/          # xUnit + FluentAssertions + WebApplicationFactory
├─ scripts/
│  ├─ run.sh                     # arranca la API
│  ├─ test.sh                    # corre tests con cobertura
│  └─ reset-db.sh                # borra couriermax.db
├─ docs/
│  └─ couriermax.postman_collection.json
└─ .github/workflows/dotnet.yml  # CI: build + test + cobertura
```

---

## 8. Cómo correr los tests en local

```bash
dotnet test --configuration Release
```

Resultado esperado: **todos los tests en verde**, agrupados en:

- `CourierMax.Tests.Domain.ShipmentStateMachineTests`
- `CourierMax.Tests.Domain.ValueObjectsTests`
- `CourierMax.Tests.Application.TariffCalculatorTests`
- `CourierMax.Tests.Application.BusinessDayCalculatorTests`
- `CourierMax.Tests.Application.ShipmentServiceTests`
- `CourierMax.Tests.Api.ShipmentsApiTests`

---



## 9. Posibles mejoras (fuera del alcance)

- Sustituir SQLite por SQL Server / Postgres en producción.
- Sustituir `EnsureCreated` por migraciones versionadas (`dotnet ef migrations`).
- Autenticación JWT para identificar al `actorId` real (hoy se usa
  `User.Identity.Name` o `"system"`).
- Background job que calcule `overdue` periódicamente y dispare notificaciones.
- Versionado de la API con namespaces separados (`api/v2/...`).
- Concurrencia optimista con `RowVersion` para evitar asignaciones dobles.
