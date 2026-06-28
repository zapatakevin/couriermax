# Decisiones arquitectónicas (ADR)

## ADR-001 — Clean Architecture en 4 proyectos

**Estado:** Aceptado.

**Contexto:**
El enunciado entrega 6 RFs y 5 RNs altamente acoplados. `Shipment` es el
agregado central y casi todo lo modelado gira alrededor de él.

**Decisión:**
Adoptar Clean Architecture con cuatro proyectos:
`Domain` (sin dependencias), `Application` (depende de Domain),
`Infrastructure` (depende de Application + Domain) y `Api` (depende de
Infrastructure). Esto facilita aplicar DIP y testear el dominio sin EF.

**Consecuencias:**
- (+) Separación clara de responsabilidades.
- (+) El dominio se prueba sin arrancar la web.
- (+) Es trivial añadir otra implementación de `ICityRepository` (por ejemplo,
  un cliente HTTP a un servicio externo).
- (-) Más boilerplate que un vertical slice. Se compensa con el plazo de 3 días
  del enunciado, donde la profundidad en domain model es más valorada.

## ADR-002 — EF Core + SQLite

**Estado:** Aceptado.

**Decisión:** EF Core 8 con SQLite por defecto; índice único sobre
`TrackingCode`, `Plate`, `Identification` y `Cities.Name`.

**Consecuencias:**
- (+) Persistencia real entre reinicios (el evaluador puede inspeccionar
  `couriermax.db`).
- (+) Sin dependencias externas (Docker, servidor de BD).
- (-) `EnsureCreated` se usa por simplicidad; en un proyecto real migraría a
  `dotnet ef migrations`.

## ADR-003 — Middleware centralizado de errores

**Estado:** Aceptado.

**Decisión:** Un único `ExceptionHandlingMiddleware` traduce las excepciones
de dominio a ProblemDetails. Los controllers no tienen bloques try/catch.

**Consecuencias:**
- (+) Cumple explícitamente el requisito del enunciado.
- (+) Permite responder con códigos consistentes (400/404/409/500).
- (-) Si se añaden tipos de excepción hay que actualizar el middleware. Se
  considera aceptable.

## ADR-004 — Tarifa y días hábiles como servicios puros

**Estado:** Aceptado.

**Decisión:** `TariffCalculator` y `ColombianBusinessDayCalculator` son
servicios sin estado, sin EF, sin reloj (salvo `IClock` cuando aplica). El
conjunto de festivos vive en una constante del archivo.

**Consecuencias:**
- (+) Tests unitarios rápidos y deterministas.
- (+) La lógica de negocio puede migrarse a un microservicio sin reescritura.

## ADR-005 — Tests con doble propósito

**Estado:** Aceptado.

**Decisión:**
- Unit tests de dominio (sin EF ni web).
- Unit tests de aplicación con repositorios in-memory.
- Tests de API con `WebApplicationFactory<Program>` y SQLite en memoria.

**Consecuencias:**
- (+) Cobertura desde la capa más baja (reglas puras) hasta el borde HTTP.
- (+) Permite validar los códigos HTTP exactos del enunciado.
- (-) Los tests de API son más lentos; se mantienen en un único archivo para
  mantener el tiempo total del suite < 30s.
