# Guía para defender la prueba en entrevista

> Documento de apoyo para el candidato. Resume **cómo explicar el proyecto
> CourierMax** en una entrevista técnica: qué decir primero, qué profundizar
> cuando pregunten, qué decisiones defender y cuáles admitir como trade-offs.

---

## 1. Apertura (30 segundos)

> "Implementé una API REST en **.NET 8 con Clean Architecture** para gestionar
> el ciclo de vida de envíos de CourierMax. Cubre los **6 requerimientos
> funcionales** y las **5 reglas de negocio** del enunciado. Tiene **53 tests
> automatizados** que validan tanto las reglas puras de dominio como el
> pipeline HTTP completo. **Persistencia con EF Core + SQLite**, sin servicios
> externos."

Después de esa intro, lo normal es que pregunten algo tipo:

- *"¿Cómo está organizado?"*
- *"¿Por qué esta arquitectura?"*
- *"Enséñame el código de..."*

---

## 2. Arquitectura en 30 segundos (lo que siempre debes poder dibujar)

```
┌─────────────────────────────────────────────────────────────┐
│  Api (Controllers + Middleware + Swagger + Serilog)        │
│  ↑ depende de Infrastructure                               │
├─────────────────────────────────────────────────────────────┤
│  Infrastructure (EF Core + SQLite + Repos + Seed + DI)     │
│  ↑ depende de Application                                  │
├─────────────────────────────────────────────────────────────┤
│  Application (Services + DTOs + Interfaces / Abstractions) │
│  ↑ depende de Domain                                       │
├─────────────────────────────────────────────────────────────┤
│  Domain (Entidades + Value Objects + Enums + Excepciones)  │
│  0 dependencias externas                                   │
└─────────────────────────────────────────────────────────────┘
```

**Regla de oro:** *las dependencias apuntan hacia el dominio*. El dominio
no sabe que existe EF, SQLite o ASP.NET.

---

## 3. Mapeo rápido: cada RF vive en…

| Requerimiento | Dónde mirar | Tiempo en explicarlo |
|---|---|---|
| RF-01 Crear envío | `ShipmentService.CreateAsync` + `TrackingCode.Generate` | 1 min |
| RF-02 Estados | `Shipment.AssignTo / StartTransit / MarkDelivered / Cancel` | 2 min |
| RF-03 Asignación | `ShipmentService.AssignAsync` + `EnsureCapacity` | 1 min |
| RF-04 Tarifa | `TariffCalculator.Calculate` | 1 min |
| RF-05 Atrasados | `ShipmentService.ListOverdueAsync` + `BusinessDayCalculator` | 1 min |
| RF-06 Métricas | `ShipmentService.GetDriverMetricsAsync` | 1 min |

---

## 4. Principios SOLID — cómo los apliqué y dónde demostrarlos

| Principio | Cómo se aplica | Fichero para enseñar |
|---|---|---|
| **S** Single Responsibility | `TariffCalculator` solo calcula; `BusinessDayCalculator` solo cuenta días hábiles; `ShipmentService` orquesta. | `src/CourierMax.Application/Services/` |
| **O** Open/Closed | `IBusinessDayCalculator` permite cambiar la lógica de festivos sin tocar el servicio. | `BusinessDayCalculator.cs` |
| **L** Liskov | Todas las entidades heredan de `Entity` con igualdad por identidad. | `src/CourierMax.Domain/Common/Entity.cs` |
| **I** Interface Segregation | Repos separados por agregado: `IShipmentRepository`, `IVehicleRepository`, etc. | `src/CourierMax.Application/Abstractions/Repositories.cs` |
| **D** Dependency Inversion | `ShipmentService` recibe `IShipmentRepository`, `ITariffCalculator`, `IClock`, `IUnitOfWork`. | constructor de `ShipmentService` |

### Si preguntan "muéstrame DRY":

- Validación de teléfono en **un solo lugar** (`PhoneNumber.Create`).
- Cálculo de tarifa **un solo método** (`TariffCalculator.Calculate`).
- Mensajes de error tipados en `DomainException` (no strings repetidos).

### Si preguntan "muéstrame KISS":

- **No usé MediatR ni CQRS**. En 3 días de plazo, agregar MediatR habría
  sido sobreingeniería. Los casos de uso son métodos directos del servicio.
- **No hice migraciones versionadas**. Usé `EnsureCreated` porque la prueba
  no requiere versionado de esquema. En un proyecto real, sí lo haría.

---

## 5. Reglas de negocio (RN) — dónde viven

| Regla | Implementación |
|---|---|
| **RN-01** Capacidad + balanceo | `ShipmentService.AutoAssignAsync` ordena por carga actual; `EnsureCapacity` valida. |
| **RN-02** Días hábiles | `ColombianBusinessDayCalculator` con `HashSet<DateOnly>` de festivos. |
| **RN-03** Cancelación | `Shipment.Cancel` exige motivo ≥ 5 chars y libera vehículo/conductor. |
| **RN-04** Validaciones | Value Objects: `PhoneNumber`, `PackageDimensions`, `Address`. |
| **RN-05** Tracking único | Índice único en BD + verificación previa en `ExistsTrackingCodeAsync`. |

---

## 6. Manejo de errores — el punto fuerte

Un único `ExceptionHandlingMiddleware` traduce excepciones de dominio a
**ProblemDetails** con códigos HTTP coherentes:

| Excepción | HTTP | Cuándo |
|---|---|---|
| `ValidationException` | 400 | Datos inválidos en input |
| `NotFoundException` | 404 | Recurso inexistente |
| `BusinessRuleException` | 409 | Estado ilegal, capacidad excedida, etc. |
| (cualquier otra) | 500 | Log + respuesta genérica |

**Por qué centralizado:** los controllers quedan limpios (cero `try/catch`).
Si añades un tipo de excepción, solo tocas el middleware.

**Por qué log en 500 pero no en 4xx:** los 4xx son errores del cliente y no
necesitan atención; los 5xx sí.

---

## 7. Testing — la pirámide

```
        ┌─────────────────┐
        │  7 tests API    │  WebApplicationFactory + SQLite :memory:
        │  (smoke E2E)    │  códigos HTTP correctos
        ├─────────────────┤
        │ 24 tests App    │  In-memory repos, IClock fijo
        │  (servicios)    │  reglas de negocio + tarifa + días hábiles
        ├─────────────────┤
        │ 22 tests Domain │  máquina de estados, value objects
        │  (puros)        │  sin EF, sin web, ultra rápidos
        └─────────────────┘
```

Total: **53 tests, < 1 segundo**.

### Si preguntan "¿cómo testeaste [X]?":

- *"¿Tarifa?"* → `TariffCalculatorTests` con casos del enunciado.
- *"¿Máquina de estados?"* → `ShipmentStateMachineTests` (10 tests).
- *"¿Días hábiles?"* → `BusinessDayCalculatorTests` (incluye el caso del
  enunciado: viernes → lunes = 1 día hábil).
- *"¿API?"* → `ShipmentsApiTests` verifica 201/400/404/409 con la API real.

---

## 8. Decisiones que defiendo a muerte

1. **Clean Architecture sobre Vertical Slices.** Los RF están acoplados
   entre sí (RF-02 toca casi todo); las capas dejan más claro dónde vive
   cada responsabilidad que features-folder.

2. **EF Core + SQLite.** Persistencia real sin fricción de Docker/SqlServer.
   El evaluador puede abrir `couriermax.db` y ver los datos.

3. **Middleware centralizado de errores.** Cero try/catch en controllers.
   Coherencia de códigos HTTP garantizada.

4. **Value Objects para validaciones.** El teléfono mal validado NUNCA llega
   al dominio. Validar en el borde, no en el servicio.

5. **Tests con dobles in-memory.** El servicio se testea sin EF; EF se testea
   indirectamente vía `WebApplicationFactory`. Cada nivel con la velocidad
   adecuada.

---

## 9. Trade-offs que admito abiertamente

| Decisión | Por qué | Cuándo lo cambiaría |
|---|---|---|
| `EnsureCreated` en lugar de migraciones | Velocidad de entrega. | Proyecto real con versionado de esquema. |
| No usé autenticación JWT | El enunciado no lo pidió. | Producción: añadir JWT con `actorId` real. |
| Code-First sin migraciones | Idem. | En producción usar `dotnet ef migrations`. |
| Solo SQLite | Cero dependencias. | En prod: SQL Server + connection pooling. |
| Guid para `StateTransition.Id` | Evita colisiones en EF tracking. | Si prefieres `long` incremental, usar secuencia o composite key. |

**Frase clave:** *"Esto es una prueba técnica; en producción X sería
diferente. Tomé la decisión Y porque el plazo era de 3 días y prioricé
calidad sobre cantidad."*

---

## 10. Errores que descubrí y resolví durante el desarrollo

Mencionar estos demuestra honestidad técnica:

1. **`Guid` vs `long` en `StateTransition.Id`.** Primero usé `long Id` con
   default 0; EF marcaba todas las nuevas transiciones como Modified (mismo
   PK) y el UPDATE no afectaba filas. Lo cambié a `Guid` autogenerado por BD.

2. **Owned type sin constructor público.** `PackageDimensions` era inmutable
   con `init`; EF no podía mapearlo. Añadí constructor sin parámetros +
   setters públicos y la factory `Create(...)` para las invariantes.

3. **SQLite `:memory:` no comparte conexión entre scopes.** En los tests de
   API con `WebApplicationFactory`, cada request creaba un nuevo DbContext y,
   por tanto, una nueva conexión → las tablas no existían. Lo arreglé con un
   `KeepAliveSqliteConnection` singleton.

4. **`BusinessDaysBetween` con bug off-by-one.** Devolvía 1 en lugar de 0
   para el mismo día. Lo corregí restando 1 si el día inicial es hábil.

5. **`Update(shipment)` con `DbUpdateConcurrencyException`.** El `Update`
   marcaba entidades como Modified cuando ya estaban en el change tracker.
   Aprendí que con `Include(Transitions)` no hace falta llamar `Update`.

---

## 11. Si te piden extender en vivo

Prepárate para cualquiera de estas modificaciones durante la entrevista
(de 5 a 15 minutos cada una):

| Petición | Dónde tocar | Tiempo |
|---|---|---|
| "Añade campo `priority` al envío" | Domain (`Shipment`) + EF Config + DTO + Service. | 5 min |
| "Endpoint para reasignar vehículo" | `ShipmentService.ReassignAsync` (no rompe los tests). | 5 min |
| "SLA configurable por cliente" | `ISlaPolicy` con default actual + override. | 10 min |
| "Notificar por email al entregar" | `IDeliveryNotifier` + `ShipmentService` lo invoca. | 10 min |
| "Paginación con cursor" | `ShipmentListFilter` + tests. | 10 min |

---

## 12. Frases de cierre útiles

- *"El dominio es el corazón; la infraestructura es un detalle."*
- *"Si mañana cambiamos SQLite por Cosmos DB, solo tocamos Infrastructure."*
- *"Las reglas de negocio no están en el controller ni en la BD: están en
  el dominio, donde se pueden testear sin levantar nada."*
- *"Preferí 6 RFs bien hechos a 10 a medias."* (alineado con el plazo de 3 días)
- *"Cada RF tiene al menos un test que falla si rompes la regla."*

---

## 13. Checklist pre-entrevista

- [ ] `dotnet test` en verde (53/53) en mi máquina.
- [ ] `dotnet run --project src/CourierMax.Api` arranca sin warnings.
- [ ] Swagger abre en `http://localhost:5080/swagger`.
- [ ] `couriermax.db` borrado para empezar limpio.
- [ ] Repasar mentalmente SOLID + RF ↔ clase.
- [ ] Tener este documento abierto durante la entrevista por si dudas.
- [ ] Tener Postman cargado con la colección.
