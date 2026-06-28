# Walkthrough — guion para demo en vivo (15 minutos)

> Si el entrevistador te dice *"muéstrame cómo funciona"*, sigue este guion.
> Está cronometrado: cada bloque tiene tiempo objetivo.

---

## Bloque 1 — Arrancar y entender (2 min)

**Decir:** *"Voy a arrancar la API limpia, abrir Swagger y mostrarte la
estructura de la solución."*

```bash
# 1. Borrar BD anterior para empezar limpio
./scripts/reset-db.sh

# 2. Restaurar y arrancar
dotnet run --project src/CourierMax.Api
```

**Mostrar:**
- Consola: Serilog arranca, "Now listening on http://localhost:5080"
- Abrir http://localhost:5080/swagger
- Panel **Solution Explorer** (VS) / estructura de carpetas (VS Code)

**Decir:** *"Clean Architecture en 4 proyectos. El dominio no tiene
dependencias de EF ni de ASP.NET — eso lo podemos demostrar abriendo el
csproj."*

📂 Mostrar `src/CourierMax.Domain/CourierMax.Domain.csproj` (sin PackageReferences).

---

## Bloque 2 — Caso del enunciado (3 min)

**Decir:** *"Voy a replicar exactamente el ejemplo del enunciado: Bogotá
→ Medellín, frágil, express, 5 kg. La tarifa esperada es $40.950."*

**Click en Swagger → `POST /api/v1/Shipments/quote`** (sin persistir):

```json
{
  "sender":    {"name":"Juan","phone":"3105551234","address":"Calle 100 #15-20"},
  "recipient": {"name":"Ana", "phone":"6015554321","address":"Carrera 50 #30-10"},
  "weightKg": 5,
  "lengthCm": 20, "widthCm": 20, "heightCm": 20,
  "packageType": 2, "serviceType": 1,
  "originCity": "Bogotá",
  "destinationCity": "Medellín"
}
```

**Mostrar respuesta:** `total: 40950.00` ✓

**Decir:** *"Coincide con el enunciado. Ahora lo creamos de verdad."*

**Click `POST /api/v1/Shipments`** con el mismo body.

**Mostrar:** `id: 1`, `trackingCode: "CM-XXXXXXXX"`, `status: "CREADO"`.

📂 Abrir `src/CourierMax.Application/Services/ITariffCalculator.cs` y
señalar la fórmula.

---

## Bloque 3 — Ciclo de vida completo (4 min)

**Decir:** *"Ahora vamos a pasar el envío por todo su ciclo:
asignar → iniciar tránsito → entregar."*

**Pasos en Swagger (o curl):**

1. **`POST /api/v1/Shipments/1/auto-assign`** → `status: "ASIGNADO"`,
   vehículo y conductor asignados.
2. **`POST /api/v1/Shipments/1/start-transit`** body `{"actorId":"driver-1"}`
   → `status: "EN_TRANSITO"`.
3. **`POST /api/v1/Shipments/1/deliver`** body `{"actorId":"driver-1"}`
   → `status: "ENTREGADO"`, `deliveredAt` con timestamp.

**Mostrar:** la lista de `transitions` ahora tiene 4 entradas
(Creación → Asignación → Tránsito → Entrega).

📂 Abrir `src/CourierMax.Domain/Entities/Shipment.cs` → método `MarkDelivered`
para mostrar la invariante.

---

## Bloque 4 — Validaciones y reglas (3 min)

**Decir:** *"Probemos las reglas de negocio: una cancelación y un par de
errores esperados."*

1. **Crear otro envío** y luego **cancelarlo**:
   ```
   POST /api/v1/Shipments/2/cancel
   Body: {"reason":"cliente cancela","actorId":"operator"}
   ```
   → `status: "CANCELADO"`, transiciones = 2.

2. **Cancelar el envío ya entregado** (debe fallar con 409):
   ```
   POST /api/v1/Shipments/1/cancel
   Body: {"reason":"ya no quiere","actorId":"operator"}
   ```
   → HTTP 409, `code: "CANNOT_CANCEL_DELIVERED"`.

3. **Crear envío con teléfono inválido**:
   ```
   POST /api/v1/Shipments
   sender.phone = "123"
   ```
   → HTTP 400, `errors: {"senderPhone": ["..."]}`.

4. **Crear envío con ciudad inexistente**:
   ```
   destinationCity = "Marte"
   ```
   → HTTP 409, `code: "CITY_NOT_FOUND"`.

**Decir:** *"400 para validación, 404 para no encontrado, 409 para regla
de negocio. Todo gestionado por un único middleware."*

📂 Abrir `src/CourierMax.Api/Middleware/ExceptionHandlingMiddleware.cs` y
mostrar el `switch` que mapea excepciones a códigos.

---

## Bloque 5 — Reportes (1 min)

1. **`GET /api/v1/reports/drivers/1/metrics`** → métricas del conductor Juan.
   Mostrar `totalDelivered: 1`, `slaCompliancePercent: 100`.
2. **`GET /api/v1/reports/overdue`** → puede estar vacío si entregamos rápido.

📂 Abrir `src/CourierMax.Application/Services/ShipmentService.cs` →
método `GetDriverMetricsAsync`.

---

## Bloque 6 — Tests (2 min)

**Decir:** *"Tenemos 53 tests en < 1 segundo. Vamos a verlos correr."*

```bash
dotnet test
```

**Mostrar:**
```
Correctas! - Con error: 0, Superado: 53, Omitido: 0, Total: 53, Duración: 920 ms
```

**Decir:** *"Tres niveles: dominio (sin EF), servicios (in-memory), API
(integration). Si rompo una regla de dominio, el test del dominio lo
detecta antes de tocar la BD."*

📂 Abrir `tests/CourierMax.Tests/Domain/ShipmentStateMachineTests.cs` y
mostrar un test como `Cancel_after_delivered_is_rejected`.

---

## Bloque 7 — Dónde extendería esto (1 min)

**Decir:** *"Si mañana me piden añadir X, ¿dónde tocaría?"*

- **JWT auth** → solo Api + DI.
- **SQL Server** → solo Infrastructure (connection string + provider).
- **Campo `priority`** → Domain (entity) + EF Config + DTO + Service.
- **Notificación al entregar** → interfaz `IDeliveryNotifier` + Service.

*"El dominio es el corazón. La infraestructura es un detalle."*

---

## Plan B — si algo falla en vivo

| Si pasa… | Decir… |
|---|---|
| Puerto ocupado | *"Cambio el puerto en launchSettings"* (y lo cambias). |
| Test falla | *"Bien, lo arreglamos en vivo"* — leer el error, abrir el código, parchar. |
| No tengo internet para NuGet | *"Tengo los paquetes en caché local, debería funcionar."* |
| Pregunta muy específica que no recuerdo | *"Te lo muestro en el código"* — abrir el fichero. |
| Pregunta sobre algo que no implementé | *"Lo dejé fuera del alcance por X. Si lo pidieras, lo haría así..."* |

---

## Cierre (30 s)

*"El proyecto está en GitHub. README tiene instrucciones de ejecución,
justificación arquitectónica y mapeo RF ↔ código. Los 53 tests cubren
todos los flujos de negocio y los códigos HTTP que pidió el enunciado.
Estoy contento con el balance entre alcance y calidad para 3 días."*
