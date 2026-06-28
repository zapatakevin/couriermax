# Cheat sheet — respuestas rápidas (60-90 segundos cada una)

> Para llevar impreso a la entrevista. Cada pregunta tiene la respuesta
> corta + el **fichero exacto** que puedes abrir para demostrarlo.

---

## "Háblame del proyecto"

API REST en **.NET 8 + Clean Architecture** para gestión de envíos de CourierMax
(6 RF, 5 RN). Persistencia con **EF Core + SQLite**. **53 tests** que cubren
dominio (puros), servicios (in-memory) y API (integration con
`WebApplicationFactory`).

---

## "¿Por qué Clean Architecture y no otra cosa?"

Porque los 6 RF están **acoplados entre sí** (la máquina de estados del envío
toca casi todos). En capas: el dominio no sabe de EF, EF no sabe de HTTP,
los controllers no tienen reglas. Trade-off vs Vertical Slices:后者gana
en features-folder, pero Clean Architecture deja más claro **dónde vive cada
cosa** cuando el dominio es central.

📂 `src/CourierMax.Domain/` — sin dependencias
📂 `src/CourierMax.Application/` — depende solo de Domain
📂 `src/CourierMax.Infrastructure/` — implementa interfaces
📂 `src/CourierMax.Api/` — controllers + middleware

---

## "¿Cómo aplicaste SOLID?"

| | Dónde |
|---|---|
| **S** | Cada clase tiene una responsabilidad (`TariffCalculator` solo calcula). |
| **O** | `IBusinessDayCalculator` permite cambiar la lógica de festivos. |
| **L** | `Entity` base con igualdad por identidad, todas las entidades la heredan. |
| **I** | Repos separados: `IShipmentRepository`, `IVehicleRepository`, etc. |
| **D** | `ShipmentService` depende de `IShipmentRepository`, no de `ShipmentRepository`. |

📂 `src/CourierMax.Application/Abstractions/Repositories.cs`

---

## "¿Y DRY/KISS?"

- **DRY**: validación de teléfono en un solo lugar (`PhoneNumber.Create`),
  festivos en una constante, tarifa en un solo método.
- **KISS**: **no usé MediatR ni CQRS** — para 3 días de plazo sería
  sobreingeniería. Los casos de uso son métodos del servicio.

---

## "Muéstrame la regla de negocio más interesante"

La **máquina de estados del envío**:

```csharp
// src/CourierMax.Domain/Entities/Shipment.cs
public void Cancel(string reason, string actorId, DateTime now)
{
    if (Status == ShipmentStatus.ENTREGADO)
        throw new BusinessRuleException("CANNOT_CANCEL_DELIVERED",
            "No se puede cancelar un envío que ya fue entregado.");
    // ...
}
```

Lo bueno es que la regla vive **en el dominio**, no en el controller ni en la
BD. Y está cubierta por 10 tests en `ShipmentStateMachineTests`.

---

## "¿Cómo manejás los errores?"

Un único `ExceptionHandlingMiddleware` traduce excepciones a ProblemDetails:

| Excepción | HTTP |
|---|---|
| `ValidationException` | 400 |
| `NotFoundException` | 404 |
| `BusinessRuleException` | 409 |
| otras | 500 |

📂 `src/CourierMax.Api/Middleware/ExceptionHandlingMiddleware.cs`

Los controllers tienen **cero try/catch**. Si añades un tipo nuevo, solo
tocas el middleware.

---

## "¿Cómo testeaste?"

Tres niveles:

1. **Dominio (22 tests)** — sin EF, sin web, ultra rápidos.
2. **Servicios (24 tests)** — con repositorios in-memory y `IClock` fijo
   (para determinismo).
3. **API (7 tests)** — `WebApplicationFactory<Program>` + SQLite `:memory:`
   con `KeepAliveSqliteConnection` singleton.

Total **53 tests en < 1 segundo**.

---

## "¿Cómo calculás la tarifa?"

```csharp
// src/CourierMax.Application/Services/ITariffCalculator.cs (extracto)
var baseFee = BaseFees[service];                       // 15000 express
var extraKg = Math.Max(0, weightKg - 2m);              // 3 kg
var weightFee = extraKg * 1500m;                       // 4500
var subtotal = baseFee + weightFee + distance;         // 31500
var surcharge = subtotal * Surcharges[package];       // 9450 (30% frágil)
var total = subtotal + surcharge;                      // 40950
```

Para Bogotá→Medellín frágil express 5kg → **40950**, exactamente lo del
enunciado. Test: `TariffCalculatorTests.Fragil_adds_30_percent_over_subtotal`.

---

## "¿Cómo manejás los festivos colombianos?"

`HashSet<DateOnly>` con los 12 festivos de 2026 hardcoded como constante
privada en `ColombianBusinessDayCalculator`. Viernes → lunes = 1 día hábil.
Test que cubre esto: `BusinessDayCalculatorTests.Friday_to_monday_counts_as_one_business_day`.

En un proyecto real se leería de un calendario, pero el alcance de la prueba
no lo justifica.

---

## "¿Por qué SQLite y no SQL Server o Postgres?"

- Cero fricción: no requiere Docker ni servidor externo.
- Persistencia real entre reinicios.
- El evaluador puede abrir `couriermax.db` y ver los datos.

Cambiar a SQL Server tocaría **solo Infrastructure** (DI + connection string).

---

## "¿Qué pasa si dos conductores asignan el mismo envío a la vez?"

Race condition real que **no resolví** porque el alcance de la prueba no lo
exige y agregaría concurrencia optimista con `RowVersion` + manejo de
`DbUpdateConcurrencyException`. **Lo menciono como trade-off honesto.**

Si lo pidieran: añadir `b.Property(x => x.RowVersion).IsRowVersion()` y
mapear la excepción en el middleware.

---

## "¿Qué cambiarías con más tiempo?"

| Cambio | Razón |
|---|---|
| Migraciones versionadas (`dotnet ef migrations`) | Versionado real del esquema. |
| Autenticación JWT | El `actorId` ahora es "system" o `User.Identity.Name`. |
| Concurrencia optimista (`RowVersion`) | Evitar asignaciones dobles. |
| Background job para SLA | Calcular atrasados cada N minutos y notificar. |
| Versionado de API (`/api/v2/...`) | Si cambian contratos. |

---

## "Enséñame un test"

```csharp
// tests/CourierMax.Tests/Domain/ShipmentStateMachineTests.cs
[Fact]
public void Cancel_after_delivered_is_rejected()
{
    var s = NewShipment();
    s.AssignTo(1, 2, DateTime.UtcNow);
    s.StartTransit(DateTime.UtcNow, "x");
    s.MarkDelivered(DateTime.UtcNow, "x");
    var act = () => s.Cancel("cliente ausente", "x", DateTime.UtcNow);
    act.Should().Throw<BusinessRuleException>().WithMessage("*entregado*");
}
```

Puro C#, sin EF, sin HTTP. Lee el test y entiende la regla.

---

## Frase de emergencia si te bloqueas

> *"No me acuerdo del detalle exacto, pero te lo muestro en el código."*

Y abres el fichero. Es lo que haría un developer real.
