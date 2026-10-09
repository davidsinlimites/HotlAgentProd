# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Proyecto

HotelAgentIA: agente de IA para huéspedes de un hotel (.NET 10, C#), con web (React) y móvil (React Native). Ruta corta a producción con **Agent Framework directo, sin Semantic Kernel**, PostgreSQL y modelo en Azure OpenAI/Foundry. **`PLAN.md` es el plan vigente** (fases A-E, stack de frontend, riesgos); `HotelAgentIA plan de aprendizaje y construcción.docx` es solo referencia (sobre todo el Bloque 5 de producción). **`ESTADO.md` es la fuente de verdad** (fase actual, decisiones vigentes, equivalencias plan→código, limitaciones, siguiente paso): léelo al empezar y actualízalo al cerrar cada fase, que termina con el proyecto compilando y un commit que diga qué se aprendió. El usuario escribe el código en VS Code; Claude revisa o completa si se pide. Repositorio: https://github.com/davidsinlimites/HotlAgentProd.

## Comandos

- Compilar: `dotnet build HotelAgent.slnx`
- Consola (banco de pruebas): `dotnet run --project src/HotelAgentIA.Console`
- API de prueba (fase A): `dotnet run --project src/HotelAgentIA.Api`; endpoints `/health`, `/stream`, `/modelo`.
- No hay proyecto de tests todavía; el monorepo `frontend/` (pnpm: Vite + React + TS + Tailwind, Expo) tampoco existe.
- Los nombres de paquetes y métodos de Agent Framework y Foundry se verifican en la documentación oficial antes de escribir código; lo ya verificado está en `docs/FASE-A-VALIDACION.md`.
- Secretos: el modelo se configura con `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_API_KEY` y `AZURE_OPENAI_DEPLOYMENT` en un `.env` en la raíz (ignorado por git y por `.claudeignore`). No lo leas, no lo imprimas ni lo añadas a commits; para saber si está relleno, cuenta caracteres sin mostrar valores.

## Arquitectura

Cinco proyectos en `src/`, con regla de capas estricta: **Domain no depende de nadie; Application → Domain; Infrastructure → Application y Domain**; Console referencia Application e Infrastructure. `Api` es hoy una prueba de concepto autónoma (no referencia las demás capas); en la fase C pasará a referenciar Application e Infrastructure. La flecha nunca va al revés (los contratos viven en Domain por inversión de dependencias).

- `Domain`: modelos como `sealed record` posicionales (`Hotel/`, `Memoria/`) y contratos (`IRepositorioInfoHotel`, `IAlmacenMemoriaClaveValor`, `IMemoriaVectorial`).
- `Application`: servicios sobre los contratos (`ServicioPreferencias`, `ServicioConversacion`) y `ClavesMemoria`.
- `Infrastructure`: implementaciones (hoy solo el almacén clave-valor en memoria; PostgreSQL con EF Core vendrá después).

Memoria por huésped: aislamiento estructural. Solo `ClavesMemoria` construye claves (`huesped:{id}:...`, minúsculas, prefijo terminado en `:`, rechaza ids vacíos o con `:`). El checkout borra todo con `EliminarPorPrefijoAsync`. `IMemoriaVectorial.BuscarAsync` recibe `huespedId`. El huésped sale del token, nunca del modelo; herramientas de solo lectura en el MVP.

## Convenciones

- `Directory.Build.props` en la raíz fija net10.0, ImplicitUsings, Nullable y `TreatWarningsAsErrors` (p. ej. CS1998 es error: usa `Task.FromResult` en vez de `async` sin `await`). Los csproj solo llevan referencias y paquetes.
- Código en español, sin tildes ni ñ en identificadores ni claves; los nombres de capa (Domain, Application, Infrastructure) quedan en inglés. Para nombres nuevos que el plan da en inglés, sigue la tabla de equivalencias de `ESTADO.md`.
- El compilador no verifica que carpeta y namespace coincidan: revísalo a mano.
- Comparar claves con `StringComparison.Ordinal`.
