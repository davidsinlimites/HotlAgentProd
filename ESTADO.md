# Estado de HotelAgentIA

- **Fase actual:** A (Validación): documentación verificada y API mínima creada (`HotelAgentIA.Api`); faltan probar `/modelo` con las credenciales y publicar en la prueba de 60 días de SmarterASP. Ver `PLAN.md` y `docs/FASE-A-VALIDACION.md`
- **Fases cerradas:** 0, 1, 2 (del plan original, conservadas)
- **Última sesión:** 2026-10-09
- **Repositorio:** https://github.com/davidsinlimites/HotlAgentProd (historial nuevo; el anterior se descartó)

## Cambio de rumbo (2026-10-09)
Se pasa de ruta de aprendizaje (35 fases, Semantic Kernel primero) a **ruta corta a producción con Agent Framework directo**, sin Semantic Kernel. `PLAN.md` es el plan vigente; el `.docx` queda como referencia (sobre todo el Bloque 5 de producción). Fases A-E de `PLAN.md`: validación, núcleo, API, clientes, producción.

## Cómo trabajamos
- Ciclo por fase: concepto, construcción (el usuario escribe en VS Code; Claude revisa o completa si se pide), depuración. Sin preguntas de comprobación al cerrar
- Al cerrar una fase: proyecto compilando y un commit con mensaje que diga qué se aprendió
- Claude entrega las clases completas cuando el usuario lo pide

## Decisiones vigentes
- PostgreSQL (Npgsql/EF Core; pgvector si hace falta búsqueda semántica) para datos y memoria, sustituye a MySQL; Blob Storage para archivos
- Agent Framework directo, sin Semantic Kernel (`Microsoft.Agents.AI` 1.24.0 estable; paquetes de hosting en prerelease, versiones fijadas); el modelo va en Azure OpenAI/Foundry
- Acceso al modelo por clave de API con variables `AZURE_OPENAI_ENDPOINT`, `AZURE_OPENAI_API_KEY`, `AZURE_OPENAI_DEPLOYMENT`: en local desde `.env`, en el hosting desde variables de entorno (la identidad administrada solo existe dentro de Azure). `.env` lo ignoran `.gitignore` y `.claudeignore`; nunca se sube ni lo lee Claude
- Frontend: monorepo `frontend/` con pnpm; web Vite + React + TypeScript + Tailwind; móvil Expo (React Native); Zustand + TanStack Query; cliente generado desde OpenAPI. Sin Next.js ni Express; solo librerías gratuitas
- Hosting de la API: SmarterASP confirmado en documentación (.NET 10, PostgreSQL, MySQL, WebSockets en todos los planes; cron solo en Premium). Por probar: SSE sin buffering, memoria del pool (256-512 MB en Basic/Advance), salida al modelo. Plan B: Azure App Service. Toda la memoria va a PostgreSQL porque IIS compartido recicla el proceso
- El huésped sale del token, nunca del modelo
- Herramientas de solo lectura en el MVP
- La API (Bloque 2) es el punto de entrada; la consola (`HotelAgentIA.Console`) es banco de pruebas y se mantiene durante todo el proyecto; se puede eliminar al final
- La solución se queda con el nombre `HotelAgent.slnx`
- `Directory.Build.props` en la raíz con lo común (net10.0, ImplicitUsings, Nullable, TreatWarningsAsErrors); los csproj solo llevan referencias y paquetes (en la consola solo `OutputType` y las referencias)
- Regla de capas: Domain no depende de nadie; Application → Domain; Infrastructure → Application y Domain
- Modelos de Domain como `sealed record` posicionales (inmutables, igualdad por valor)
- Menús en Word/PDF: se procesan una sola vez al cargarlos, con revisión humana, y se guardan como filas en PostgreSQL. Los vectores son solo para texto libre (políticas, preguntas frecuentes)
- Código en español, sin tildes ni ñ en identificadores ni en claves; los nombres de capa (Domain, Application, Infrastructure) se quedan en inglés
- Claves de memoria en minúsculas con formato `huesped:{id}:...`; solo `ClavesMemoria` las construye. El prefijo de un huésped termina en `:` (`huesped:30:` no coincide con `huesped:305:`)
- Borrado total de un huésped (`EliminarPorPrefijoAsync`) al hacer checkout, no al terminar cada conversación. Un prefijo vacío se rechaza
- Conversación: una sola clave por huésped (`huesped:{id}:conversacion`) con lista JSON de turnos, máximo 20, rol guardado como texto

## Equivalencias del plan (en inglés) y el código (en español)
| Plan | Código |
|---|---|
| Restaurant, OpeningHours, MenuItem | Restaurante, HorarioApertura, PlatoMenu |
| Activity, Guest, Stay | Actividad, Huesped, Estancia |
| IHotelInfoRepository | IRepositorioInfoHotel |
| ConversationRole, ConversationTurn | RolConversacion, TurnoConversacion |
| MemoryFragment, ScoredMemory | FragmentoMemoria, MemoriaPuntuada |
| IKeyValueMemoryStore | IAlmacenMemoriaClaveValor |
| IVectorMemory | IMemoriaVectorial |
| InMemoryKeyValueStore | AlmacenMemoriaClaveValorEnMemoria |
| MemoryKeys | ClavesMemoria |
| PreferenceExtractor | ExtractorPreferencias |

Para nombres nuevos que el plan da en inglés se sigue la misma convención.

## Estado del código
- Domain con modelos y contratos (fase 1):
  - `Hotel/`: Restaurante, HorarioApertura, PlatoMenu, Actividad, Huesped, Estancia, IRepositorioInfoHotel
  - `Memoria/`: RolConversacion, TurnoConversacion, FragmentoMemoria, MemoriaPuntuada, IAlmacenMemoriaClaveValor, IMemoriaVectorial
- Application (fase 2), carpeta `Memoria/`: `ClavesMemoria`, `ServicioPreferencias`, `ServicioConversacion`
- Infrastructure (fase 2), carpeta `Memoria/`: `AlmacenMemoriaClaveValorEnMemoria` (`ConcurrentDictionary`; expone `Claves` solo para el banco de pruebas)
- `HotelAgentIA.Console` referencia Application e Infrastructure; `Program.cs` ejecuta el escenario de dos huéspedes (305 y 412), con checkout del 305
- `HotelAgentIA.Api` (fase A, prueba de concepto, no referencia aún a las demás capas): `Program.cs` con `GET /health`, `GET /stream?eventos=N` (SSE, un evento por segundo) y `GET /modelo?q=` (Azure OpenAI vía `AzureOpenAIClient(...).GetChatClient(despliegue).AsAIAgent(...)`; 503 si faltan variables). Carga `.env` con DotNetEnv (`NoClobber().TraversePath()`). Paquetes: `Microsoft.Agents.AI.OpenAI` 1.24.0, `Azure.AI.OpenAI` 2.1.0, `DotNetEnv` 3.2.0. Compila sin advertencias; `/health` y `/stream` probados en local, `/modelo` sin probar (faltan credenciales en `.env`)
- `HotelAgent.slnx` referencia los 5 proyectos
- `docs/FASE-A-VALIDACION.md`: hallazgos de la validación (versiones, API verificada, límites de SmarterASP)

## Lo aprendido
- Fase 0: `Directory.Build.props` lo aplica MSBuild a todos los proyectos bajo su carpeta; una referencia circular da `MSB4006`; MSBuild solo detecta ciclos completos, el resto lo protege la disciplina
- Fase 1: el compilador no exige que la carpeta coincida con el namespace (un copy-paste dejó los 6 archivos de `Memoria/` en `Domain.Hotel`); hay que revisarlo a mano
- Fase 1: `namespace` declara dónde vive un tipo; `using` solo hace falta para tipos de otro namespace. `ImplicitUsings` ya cubre `System.*` (pero no `System.Collections.Concurrent`)
- Fase 1: `IMemoriaVectorial.BuscarAsync` recibe `huespedId` para que el aislamiento entre huéspedes sea estructural, no un filtro posterior
- Fase 1: los contratos viven en Domain por inversión de dependencias (la D de SOLID)
- Fase 2: `ClavesMemoria` es el único sitio que sabe armar claves; exige `huespedId` y rechaza vacíos y `:`, así el aislamiento es estructural y no depende de la disciplina
- Fase 2: un almacén clave-valor no tiene registros por huésped; "borrar al huésped" es borrar todas las claves que empiezan por su prefijo
- Fase 2: `ConcurrentDictionary` porque la API atenderá huéspedes a la vez; `Task.FromResult` en vez de `async` sin `await` (CS1998 es error con `TreatWarningsAsErrors`)
- Fase 2: `StringComparison.Ordinal` al comparar claves (son identificadores, no texto)
- Fase 2: Infrastructure referencia Domain para poder implementar sus interfaces; la flecha nunca va al revés
- Fase 2: con C# Dev Kit se depura con configuración dinámica (no `launch.json`); VS Code avisa si se intenta generar assets a la antigua

- Fase A: Agent Framework ya va por 1.24 estable (el plan Word decía 1.0). No incluye un almacén durable de sesiones (`AgentSessionStore`) ni de historial: los implementamos sobre PostgreSQL con `ChatHistoryProvider`
- Fase A: el id de sesión que envía el cliente no prueba propiedad; se mapea a nuestro id interno y se verifica contra el huésped del token
- Fase A: la extensión `AsAIAgent` para `ChatClient` de OpenAI necesita `using OpenAI;` y `using OpenAI.Chat;` además de `Microsoft.Agents.AI`
- Fase A: ASP.NET Core no lee `.env` por sí solo; se usa DotNetEnv, y las variables de entorno reales tienen prioridad

## Limitaciones conocidas
- `ServicioConversacion` hace leer-modificar-escribir sobre una sola clave: dos mensajes simultáneos del mismo huésped podrían pisarse. Revisar al pasar a PostgreSQL
- El almacén en memoria pierde todo al reiniciar (se resuelve con PostgreSQL)

## Experimento pendiente (fase 1, opcional)
- Añadir `using HotelAgentIA.Application;` dentro de un archivo de Domain, compilar y anotar el error; luego quitarlo

## Para fases siguientes
- Fase 5: decidir dónde vive la clave del wifi (no va en `Estancia`)
- Fase 5: representar horarios que cierran pasada la medianoche (`HorarioApertura` con `TimeOnly`)

## Ideas para v2
- Conversaciones múltiples por huésped: añadir un `conversacionId` generado por nosotros dentro del prefijo del huésped (`huesped:{id}:conversacion:{conversacionId}`). El checkout sigue borrando todo con un solo prefijo y se puede borrar un chat concreto con `EliminarAsync`
- Conservar preferencias para que el hotel decida: guardar solo estadísticas agregadas y anónimas en tablas aparte, y borrar lo individual en el checkout. Requiere consentimiento del huésped y revisar la normativa de privacidad aplicable

## Siguiente: fase A (Validación) y luego B (Núcleo)
- Fase A, pendiente: rellenar `.env`, probar `/modelo` en local; publicar la API en la prueba de 60 días de SmarterASP y repetir `/stream` y `/modelo` allí; decidir hosting (ver `docs/FASE-A-VALIDACION.md`). Sin verificar aún: `AIContextProvider` en .NET, cómo pasar el huésped a una herramienta, `RunStreamingAsync`, `pgvector` en el PostgreSQL de SmarterASP
- Fase B incluye `ExtractorPreferencias` (antes fase 3), datos en PostgreSQL, herramientas y agente

### Detalle heredado de la fase 3
- `ExtractorPreferencias`: detecta dieta, idioma y hora de desayuno en el mensaje del huésped y lo guarda con `ServicioPreferencias`
- Observar: dónde falla la detección por palabras clave (negaciones como "no soy vegetariano", sinónimos, mensajes con varias preferencias)

## Dudas
- (ninguna abierta)

## Cómo ejecutar
- Compilar: `dotnet build HotelAgent.slnx`
- Consola: `dotnet run --project src/HotelAgentIA.Console`
- API de prueba: `dotnet run --project src/HotelAgentIA.Api` (necesita `.env` en la raíz para `/modelo`)
