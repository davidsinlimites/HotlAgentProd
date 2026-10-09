# Plan: HotelAgentIA, ruta corta a producción con Agent Framework

## Contexto
El proyecto es un ejercicio de aprendizaje (plan de 35 fases, Semantic Kernel primero, migración a Agent Framework en el Bloque 3). El usuario quiere ahora llegar a producción cuanto antes: usar **Agent Framework directamente, sin pasar por Semantic Kernel**, con frontend web, app móvil (React Native, iOS y Android), modelo en Foundry y despliegue por decidir (SmarterASP como candidato). Se creará un repositorio nuevo, así que se descarta el `.git` actual.

Decisiones tomadas en esta sesión:
- Ruta corta: se omiten el Bloque 1 de SK (fases 3-10 en su forma SK) y el Bloque 3 (migración).
- Web: React + TypeScript; móvil: React Native; cliente TypeScript compartido, generado desde OpenAPI.
- Datos: PostgreSQL (con pgvector si hace falta búsqueda semántica), reemplaza a MySQL.
- Hosting API: verificar SmarterASP primero, con plan B en Azure.

## Qué se conserva del código actual
- Capas y regla de dependencias (`Directory.Build.props`, `HotelAgent.slnx`).
- Domain completo: modelos de `Hotel/` y `Memoria/` y los contratos (`src/HotelAgentIA.Domain/...`).
- Application: `ClavesMemoria`, `ServicioPreferencias`, `ServicioConversacion` (`src/HotelAgentIA.Application/Memoria/`).
- `AlmacenMemoriaClaveValorEnMemoria` como implementación de pruebas; consola como banco de pruebas.
- Decisiones de `ESTADO.md` (huésped del token, solo lectura, aislamiento por prefijo, checkout borra todo).

Cambios en decisiones: MySQL → PostgreSQL; fase de SK eliminada; `ESTADO.md` y CLAUDE.md se actualizan en el repo nuevo.

## Fases propuestas

**A. Validación (antes de escribir más código)**
1. Verificar en documentación oficial: paquetes y versiones de Agent Framework (1.0), soporte .NET 10, `AIAgent`, `AgentSession`, `AIContextProvider`, Foundry.
2. Verificar SmarterASP: versión de .NET soportada, PostgreSQL disponible, SSE sin buffering en IIS, tareas en segundo plano, HTTPS/dominio. Si falla alguna, plan B: Azure App Service o Container Apps para la API.
3. Decidir hosting de PostgreSQL (SmarterASP o gestionado, p. ej. Azure Database for PostgreSQL).

**B. Núcleo (reutiliza fases 3, 5 y 7 del plan, sin SK)**
4. `ExtractorPreferencias` (fase 3) o delegarlo al modelo vía herramienta; decidir tras probar.
5. Datos del hotel en PostgreSQL con EF Core (Npgsql), semilla de ejemplo, `IRepositorioInfoHotel` en Infrastructure.
6. Servicio `IHotelInfoService` (Application) y herramientas de solo lectura (horarios, menú, actividades, wifi) expuestas como tools de Agent Framework; el huésped viene del contexto del servidor, nunca del modelo.
7. Agente: `IChatClient` hacia Foundry + `AIAgent` con tools; `AgentSession` para conversación; `AIContextProvider` sobre `IAlmacenMemoriaClaveValor` para preferencias.
8. Modo offline: `IChatClient` falso para depurar y evaluar sin costo.

**C. API (fases 11-15 del plan)**
9. `HotelAgentIA.Api` (Minimal API), `POST /chat` y streaming SSE, OpenAPI.
10. JWT con guestId/habitación/estancia; CORS para web y móvil; rate limiting; manejo de errores; logs sin datos sensibles.
11. Memoria persistente en PostgreSQL; sessionId distinto de guestId; borrado en checkout (tarea programada).

**D. Clientes** (todo con librerías gratuitas de código abierto; stack detallado en la sección siguiente)
12. Monorepo `frontend/` con pnpm workspaces: `apps/web`, `apps/mobile`, `packages/api-client`, `packages/chat-core`.
13. `api-client`: cliente TypeScript generado desde el OpenAPI de la API.
14. Web: Vite + React + TypeScript + Tailwind, chat con streaming SSE.
15. Móvil: Expo (React Native) reutilizando `api-client` y `chat-core`; login/token del huésped.

## Stack frontend (todo gratuito / MIT)
| Necesidad | Elección | Nota |
|---|---|---|
| Build web | **Vite** + React + TypeScript (strict) | SPA pura. La API es .NET, así que no hace falta servidor de Node |
| Estilos web | **Tailwind CSS** + **shadcn/ui** (Radix) | Los componentes se copian al repo; sin licencia |
| Estado de cliente | **Zustand** | Sesión, token, mensajes en curso, UI. Pequeño y funciona igual en React Native |
| Estado de servidor | **TanStack Query** | Caché de datos del hotel (menús, horarios). El chat en streaming va aparte, en un store de Zustand |
| Streaming | `fetch` + `ReadableStream` (o `@microsoft/fetch-event-source` por el `POST` con cabecera `Authorization`) | `EventSource` nativo no permite POST ni cabeceras |
| Rutas | **React Router** | |
| Formularios/validación | **React Hook Form** + **Zod** | Zod también valida respuestas de la API |
| Cliente API | **openapi-typescript** + `openapi-fetch` (u Orval) | Tipos generados del OpenAPI; un paquete compartido web/móvil |
| Móvil | **Expo** + **React Native** + **Expo Router** | Compilación local gratuita; los servicios de build en la nube (EAS) son opcionales |
| Estilos móvil | **NativeWind** (Tailwind en RN) | Mismas clases que la web |
| Pruebas | **Vitest** + Testing Library; **Playwright** para E2E web | |
| Calidad | ESLint, Prettier, `tsc --noEmit` en CI | |
| i18n | **i18next** | El hotel tendrá huéspedes de varios idiomas |

### Next.js y Express: recomendación
- **Next.js** es gratuito (MIT); lo que puede costar es alojarlo en Vercel. Aun así no aporta valor aquí: no hay SEO que cuidar (es una app tras login) y ya existe un backend. Además complica el despliegue en SmarterASP (necesita Node). Se descarta.
- **Express** tampoco hace falta: la API es ASP.NET Core. Un segundo backend duplicaría autenticación y reglas. Se descarta.
- La web se compila a archivos estáticos (`vite build`) que se sirven desde cualquier hosting, incluido SmarterASP o el propio ASP.NET (`UseStaticFiles`).

### Costos que no son librerías (inevitables para la app móvil)
- Apple Developer Program (~99 USD/año) y Google Play (~25 USD, pago único) para publicar en las tiendas.
- Alternativa gratuita inicial: PWA de la web mientras se valida con el hotel.

**E. Producción (Bloque 5 del plan)**
15. Seguridad del agente (prompt injection, pruebas de abuso), privacidad y retención, evaluación con conversaciones fijas, observabilidad y costos, "cuando no sabe", entrega al hotel.
16. Despliegue: CI/CD (GitHub Actions), entornos dev/prod, secretos (user-secrets local; Key Vault o variables del hosting).

## Riesgos y preguntas abiertas
- SmarterASP es IIS compartido: SSE, procesos largos y .NET 10 pueden no estar soportados.
- Memoria propia en PostgreSQL frente a la memoria gestionada de Foundry (preview).
- Región y normativa de privacidad aplicable al hotel.
- El plan Word y `ESTADO.md` deben reescribirse para reflejar la ruta corta.

## Verificación
- Fase A: documento de hallazgos con versiones y límites confirmados.
- `dotnet build HotelAgent.slnx` y consola con huéspedes 305/412 funcionando offline tras cada fase.
- API: prueba manual con JWT de dos huéspedes; pedir el wifi de otra habitación debe fallar.
- Clientes: flujo de chat extremo a extremo en web y en emulador móvil.
