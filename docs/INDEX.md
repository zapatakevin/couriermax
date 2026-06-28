# Documentación de CourierMax

> Toda la documentación del proyecto en un solo lugar. Si vas a una
> entrevista, **arrancá por acá**.

---

## 🚀 Para el entrevistador / evaluador

| Documento | Para qué |
|---|---|
| **[README.md](../README.md)** | Cómo ejecutar + justificación + mapeo RF↔código |
| **[GUIA_ENTREVISTA.md](GUIA_ENTREVISTA.md)** | Cómo defender el proyecto en una entrevista técnica |
| **[CHEAT_SHEET.md](CHEAT_SHEET.md)** | Respuestas rápidas de 60-90s a preguntas típicas |
| **[WALKTHROUGH.md](WALKTHROUGH.md)** | Guion cronometrado para demo en vivo de 15 minutos |
| **[PREGUNTAS_FRECUENTES.md](PREGUNTAS_FRECUENTES.md)** | 30 Q&A reales con respuesta modelo |

## 🏗 Para entender el diseño

| Documento | Para qué |
|---|---|
| **[ARCHITECTURE.md](ARCHITECTURE.md)** | 5 ADRs (decisiones arquitectónicas documentadas) |
| **[couriermax.postman_collection.json](couriermax.postman_collection.json)** | 10 endpoints listos para Postman |

---

## 📖 Orden de lectura sugerido

### Si tenés 5 minutos
1. [README.md](../README.md) → sección "Resumen de la solución"
2. [CHEAT_SHEET.md](CHEAT_SHEET.md) → las primeras 4 preguntas

### Si tenés 15 minutos
1. [README.md](../README.md) → completo
2. [ARCHITECTURE.md](ARCHITECTURE.md)
3. [GUIA_ENTREVISTA.md](GUIA_ENTREVISTA.md) → secciones 2 (arquitectura) y 5 (reglas de negocio)

### Si vas a una entrevista
1. **Antes** (noche anterior):
   - [GUIA_ENTREVISTA.md](GUIA_ENTREVISTA.md) → completa
   - [CHEAT_SHEET.md](CHEAT_SHEET.md) → impresa o en otra ventana
   - [PREGUNTAS_FRECUENTES.md](PREGUNTAS_FRECUENTES.md) → 30 Q&A

2. **Durante** (si te dejan mostrar código):
   - [WALKTHROUGH.md](WALKTHROUGH.md) → guion de 15 min

3. **Si te preguntan trade-offs**:
   - [GUIA_ENTREVISTA.md](GUIA_ENTREVISTA.md) → sección 9 "Trade-offs que admito abiertamente"

---

## 🎯 Resumen ejecutivo (30 segundos)

**CourierMax** es una API REST en **.NET 8 + Clean Architecture** para
gestionar el ciclo de vida de envíos de una empresa de courier.

- **4 capas:** Domain (0 dependencias) → Application → Infrastructure → Api.
- **6 RF + 5 RN** del enunciado, todos implementados.
- **Persistencia:** EF Core 8 + SQLite (sin Docker ni servicios externos).
- **53 tests** automatizados en < 1 segundo (unit + integration).
- **Manejo de errores** centralizado (un middleware traduce excepciones a HTTP).
- **Listo para correr:** `dotnet restore && dotnet test && dotnet run`.
