# Preguntas frecuentes del entrevistador (Q&A)

> 30 preguntas reales con respuesta modelo. Ordenadas por probabilidad
> (de más típica a más técnica).

---

## Generales

### 1. "¿Cuánto tiempo te tomó?"

*"3 días, según el plazo del enunciado. Los primeros días los dediqué a
leer el enunciado y diseñar el dominio. El segundo día fue implementar
servicios + tests. El tercero, API + integración + README."*

### 2. "¿Usaste IA para hacerlo?"

*"Sí, usé Codex como asistente de boilerplate y para repasar sintaxis de
EF Core 8. Las decisiones de diseño (arquitectura, naming, separación de
capas) las tomé yo. Cada bloque de código que generé lo revisé y lo
adecu al estilo del proyecto."*

### 3. "¿Qué fue lo más difícil?"

*"El manejo de `StateTransition` con EF. Primero usé `long Id` y al insertar
varios transitions del mismo envío en una transacción, EF los marcaba como
Modified (mismo PK = 0) en lugar de Added. Lo cambié a `Guid`
autogenerado por la BD y se resolvió. Lo cuento en la guía de entrevista
porque muestra que depuré el problema hasta la raíz."*

### 4. "¿Qué dejarías fuera?"

*"Tres cosas: autenticación JWT (no la pedía el enunciado), migraciones
versionadas de EF (usé `EnsureCreated` por velocidad) y concurrencia
optimista con `RowVersion` (no estaba en el alcance)."*

---

## Arquitectura

### 5. "¿Por qué 4 proyectos y no uno?"

*"Cada capa tiene una responsabilidad y un ritmo de cambio distinto.
El dominio casi nunca cambia. La infraestructura cambia cuando cambias
de BD. La API cambia cuando cambias de framework web. Tenerlas separadas
permite testear el dominio sin EF ni HTTP."*

### 6. "¿Y por qué no MediatR o CQRS?"

*"En 3 días de plazo, MediatR agrega una capa de indirección que no
aporta valor cuando los casos de uso son métodos directos del servicio.
Si el proyecto creciera a 20+ features con handlers reusables, lo
consideraría. KISS."*

### 7. "Si mañana cambias de SQLite a SQL Server, ¿qué tocas?"

*"Solo Infrastructure. La connection string, el provider en DI, y los
detalles específicos de SQL Server (por ejemplo, `IsRowVersion()` o
secuencias). El dominio y la API no se enteran."*

### 8. "¿Cómo comunicas las capas?"

*"El dominio expone entidades. Application expone DTOs y depende del
dominio via interfaces (`IShipmentRepository`, no `ShipmentRepository`).
Infrastructure implementa esas interfaces. La API recibe DTOs, no
entidades — eso evita que EF se filtre a la respuesta HTTP."*

---

## Diseño / SOLID

### 9. "¿Dónde está el SRP en tu código?"

*"`TariffCalculator` solo calcula tarifas. `ColombianBusinessDayCalculator`
solo cuenta días hábiles. `ShipmentService` orquesta casos de uso.
`ExceptionHandlingMiddleware` solo traduce excepciones a HTTP. Si una clase
tiene más de una razón para cambiar, la parto."*

### 10. "Muéstrame DIP"

📂 `src/CourierMax.Application/Services/ShipmentService.cs`, constructor:

```csharp
public ShipmentService(
    IShipmentRepository shipments,    // interfaz, no concreción
    ITariffCalculator tariff,
    IClock clock,
    ...
)
```

*"Si quiero cambiar EF por Dapper, escribo un nuevo repositorio y lo
registro en DI. El servicio no se entera."*

### 11. "¿Cómo aplicaste DRY?"

*"Validación de teléfono solo en `PhoneNumber.Create`. Festivos en una
constante. La fórmula de tarifa en un solo método. Si una regla aparece
en 3 sitios, la extraigo."*

### 12. "¿Value Object vs Entity? ¿Cuándo usás cada uno?"

*"Entity tiene identidad (`Id`). Value Object no: dos teléfonos con
los mismos 10 dígitos son el mismo teléfono. Uso records sellados para
VOs: `PhoneNumber`, `Address`, `PackageDimensions`, `TrackingCode`."*

---

## Implementación

### 13. "¿Cómo generás el código de rastreo único?"

📂 `TrackingCode.Generate(rng)` produce `CM-XXXXXXXX`. El servicio verifica
con `ExistsTrackingCodeAsync` antes de aceptar. Si hay colisión (1 en
100M), reintenta hasta 5 veces. El índice único en BD es la red de
seguridad final.

### 14. "¿Cómo evitás dos envíos con el mismo tracking?"

*"Dos capas: chequeo previo en el servicio (rápido, baja colisión) +
índice único en BD (autoridad final). Si dos requests pasan el check
previo simultáneamente, la BD rechaza el segundo con `DbUpdateException`."*

### 15. "¿Cómo se asigna automáticamente el vehículo con menor carga?"

📂 `ShipmentService.AutoAssignAsync` ordena los vehículos activos por
carga actual (peso total de envíos ASIGNADO + EN_TRANSITO) y prueba con
cada uno hasta que la capacidad alcance. Si ninguno puede, lanza
`BusinessRuleException("NO_VEHICLE_AVAILABLE")`.

### 16. "¿Qué pasa si el envío excede TODOS los vehículos?"

*"Devuelvo 409 con código `NO_VEHICLE_AVAILABLE` y un mensaje claro.
El cliente puede reintentar o dividir el envío. No hay fallback mágico."*

### 17. "¿Cómo calculás los días hábiles?"

📂 `ColombianBusinessDayCalculator.BusinessDaysBetween`. `HashSet<DateOnly>`
con los 12 festivos colombianos 2026. Excluye sábado/domingo. Caso del
enunciado (viernes → lunes = 1 día hábil) cubierto por test.

### 18. "¿Por qué no lees los festivos de un calendario externo?"

*"Para 3 días de plazo, hardcoded es suficiente. Si fuera producción,
leería de una API de festivos o un archivo de configuración cargado
al arranque. Lo dejé aislado detrás de `IBusinessDayCalculator`
para que cambiarlo sea trivial."*

---

## Errores / robustez

### 19. "¿Cómo manejás errores?"

📂 `ExceptionHandlingMiddleware`. Mapea:
- `ValidationException` → 400
- `NotFoundException` → 404
- `BusinessRuleException` → 409
- Otros → 500 + log

*"Los controllers tienen cero try/catch. Si añades un tipo de excepción,
solo tocas el middleware."*

### 20. "¿Y si la BD se cae?"

*"El middleware captura la excepción, devuelve 500 con un mensaje
genérico (no leak de detalles internos) y log con `LogError`. El cliente
ve un 500 y puede reintentar. En producción añadiría un circuit breaker
(Polly) y reintentos exponenciales."*

### 21. "¿Cómo evitás N+1 en el listado de envíos?"

*"Uso `Include(s => s.Transitions)` cuando cargo un envío individual.
Para listados paginados uso `AsNoTracking()` y solo cargo los campos
necesarios (no las transiciones). Para métricas por conductor, cargo
todos los envíos del conductor y proceso en memoria — aceptable porque
un conductor tiene pocos envíos asignados."*

---

## Testing

### 22. "¿Cómo testeás la lógica de negocio?"

📂 `tests/CourierMax.Tests/Domain/ShipmentStateMachineTests.cs`.

*"Pruebo el dominio sin EF ni HTTP. La entidad `Shipment` se testea como
cualquier clase C#. Ejemplo: `Cancel_after_delivered_is_rejected` —
construyo un envío, lo llevo a ENTREGADO, intento cancelarlo y verifico
que lance `BusinessRuleException`."*

### 23. "¿Cómo testeás el servicio sin BD real?"

*"Repositorios in-memory (`InMemoryShipmentRepo` etc.) que implementan
las mismas interfaces. El servicio recibe las interfaces en el
constructor, así que en test le paso los dobles. Ventaja: tests rápidos
y deterministas (uso `TestClock` con fecha fija)."*

### 24. "¿Y la API?"

📂 `tests/CourierMax.Tests/Api/ShipmentsApiTests.cs`.

*"`WebApplicationFactory<Program>` arranca toda la pipeline (controllers,
middleware, DI) con SQLite `:memory:`. Pruebo los códigos HTTP que pidió
el enunciado: 200, 201, 400, 404, 409."*

### 25. "¿Cobertura de código?"

*"Con `coverlet.collector` que ya está configurado. No es un objetivo
autoimpuesto del 100%, pero cada RF y cada RN tiene al menos un test que
falla si rompo la regla."*

---

## Despliegue / producción

### 26. "¿Cómo lo desplegarías?"

*"`Dockerfile` multi-stage: SDK para build, ASP.NET runtime para ejecución.
Variables de entorno para connection string. En producción usaría
Azure Container Apps o AWS ECS detrás de un load balancer.
Migraciones versionadas en lugar de `EnsureCreated`."*

### 27. "¿Y la seguridad?"

*"Validación de input en el borde (value objects). HTTPS en producción.
JWT para identificar al `actorId` real (hoy es 'system' o
`User.Identity.Name`). Rate limiting con `Microsoft.AspNetCore.RateLimiting`.
CORS cerrado en producción (hoy abierto para evaluación local)."*

### 28. "¿Cómo manejarías 10K requests por segundo?"

*"Lo principal: paginación obligatoria, consultas a BD optimizadas con
índices (ya hay índice único en `TrackingCode`, `Plate`, `Identification`,
`Cities.Name`). Caché de datos de referencia (ciudades, vehículos) con
`IMemoryCache`. Background job para métricas en lugar de calcularlas en
cada request. Connection pooling. CQRS con read models."*

---

## Preguntas trampa

### 29. "¿Y si te pido que lo hagas todo de nuevo?"

*"Mismo plazo, mismo alcance, pero: empezaría con los tests del dominio
(TDD inverso), porque son los que dan más confianza. Y usaría
`dotnet ef migrations` desde el día 1 para evitar el `EnsureCreated`."*

### 30. "¿Qué nota le pondrías?"

*"8/10. Cubre los 6 RF y las 5 RN con tests, la arquitectura es limpia
y la documentación está. Lo que le falta: autenticación real,
concurrencia optimista, migraciones, métricas con Prometheus, y un
Dockerfile. Pero eso es scope, no calidad."*
