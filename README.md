# HotelAgentIA

Agente de IA para los huéspedes de un hotel: responde sobre restaurantes, menús, actividades y la estancia del propio huésped, y recuerda sus preferencias. .NET 10 con Microsoft Agent Framework (sin Semantic Kernel), PostgreSQL y un modelo en Azure OpenAI/Foundry. Clientes web (React) y móvil (React Native) previstos.

## Estado

Fases A (validación), B (núcleo) y C (API) cerradas; siguiente, D (clientes). El detalle vive en [`ESTADO.md`](ESTADO.md) y el plan en [`PLAN.md`](PLAN.md).

## Cómo ejecutar

- Compilar: `dotnet build HotelAgent.slnx`
- Pruebas (sin red ni base de datos): `dotnet test tests/HotelAgentIA.Tests`
- API: `dotnet run --project src/HotelAgentIA.Api`. Necesita en un `.env` en la raíz (ignorado por git): `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_API_KEY`, `AZURE_OPENAI_DEPLOYMENT`, `POSTGRES_CONNECTION`, `JWT_SIGNING_KEY` (32 bytes o más) y, solo para pruebas, `DEV_TOKENS=true`
- Preparar una base nueva (migra y siembra): `dotnet run --project src/HotelAgentIA.Console -- --solo-bd`

## Estructura

`src/`: `Domain` (modelos y contratos), `Application` (servicios y agente), `Infrastructure` (EF Core, almacén de memoria, cliente del modelo), `Api` (Minimal API con JWT y SSE) y `Console` (banco de pruebas). `tests/`: xUnit.
