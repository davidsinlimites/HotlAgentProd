# Estado de HotelAgentIA

- **Fase actual:** D (clientes) en curso desde el 2026-10-13: monorepo `frontend/`, `api-client`, `chat-core` y web hechos y probados con el modelo real; falta el móvil (Expo) y cerrar la fase. Antes: C (API) cerrada el 2026-10-12. La API real está publicada y probada en el hosting con el modelo real. Ver "Para retomar", `PLAN.md` y `docs/FASE-A-VALIDACION.md`
- **Fases cerradas:** 0, 1, 2 (del plan original, conservadas), A, B y C
- **Última sesión:** 2026-10-12
- **Repositorio:** https://github.com/davidsinlimites/HotlAgentProd (historial nuevo; el anterior se descartó)

## Para retomar (leer primero)
- **Dónde nos quedamos (2026-10-12):** fases A, B y C cerradas. `src/HotelAgentIA.Api` está publicada en https://davidroblero-001-site2.ftempurl.com/ y probada allí de extremo a extremo con el modelo real (`/health`, `/health/db`, `/dev/token`, `/chat`, `/chat/stream`). 92 pruebas en `tests/HotelAgentIA.Tests` (`dotnet test tests/HotelAgentIA.Tests`). El commit de cierre de la fase C resume lo aprendido
- **Fase D, avance (2026-10-13):** `frontend/` (pnpm 12, Node 24) con `packages/api-client` (tipos generados de `openapi.json` con `pnpm --filter @hotelagent/api-client generar`, más `crearClienteApi` con `openapi-fetch`), `packages/chat-core` (parser SSE, `transmitirChat`, store Zustand vanilla `crearAlmacenChat`; 11 pruebas Vitest con `pnpm --filter @hotelagent/chat-core test`) y `apps/web` (Vite 8 + React 19 + Tailwind 4; login de demo por `/dev/token` y chat con streaming; en desarrollo el proxy de Vite reenvía `/chat`, `/dev` y `/health` a `http://localhost:55002`, y en producción `VITE_API_URL` apunta a la API y esta debe incluir el origen en `CORS_ORIGINS`). Arranque: API local con `DEV_TOKENS=true dotnet run --project src/HotelAgentIA.Api --launch-profile HotelAgentIA.Api` y `pnpm dev` en `frontend/`. Probado en el navegador contra el modelo real. **Falta:** `apps/mobile` (Expo; el `fetch` de React Native necesita streaming: usar `expo/fetch` e inyectarlo en `fetchImpl`), shadcn/ui, React Router, TanStack Query (cuando haya datos del hotel que cachear), i18next, Playwright E2E, y decidir cómo se sirve la web (estáticos en SmarterASP o `UseStaticFiles`)
- **Fechas de estancias:** `dotnet run --project src/HotelAgentIA.Console -- --renovar-estancias` las deja activas 30 días en la base de `POSTGRES_CONNECTION` (el usuario debe ejecutarlo contra la del hosting; renovar antes del 2026-10-14)
- **Fase D, siguiente paso:** móvil y cierre. (Lo que sigue es el texto original del paso.) Monorepo `frontend/` con pnpm (`apps/web`, `apps/mobile`, `packages/api-client`, `packages/chat-core`); empezar por `api-client` generado desde el OpenAPI (`/openapi/v1.json`, solo en Development) y la web (Vite + React + TS + Tailwind) con chat por SSE. Antes de escribir código, verificar en la documentación oficial las versiones y comandos de las librerías del stack, y comprobar que Node y pnpm están instalados. Contrato SSE: `POST /chat/stream` devuelve eventos `data: "<trozo de texto en JSON>"`, `event: fin` al terminar y `event: error` si falla a mitad; los errores previos al stream son JSON (400 validación, 401 sin token, 422 filtro de contenido con mensaje amable, 429 límite, 502 fallo del modelo)
- **Pendientes de decisión antes de huéspedes reales** (no bloquean la fase D, sí la producción):
  1. **Autenticación real del huésped.** `POST /dev/token` no pide contraseña: solo con `DEV_TOKENS=true`, solo para demo con datos ficticios, apagarlo antes de producción. Hay que decidir cómo se identifica el huésped y quién emite el token (el JWT lleva `sub` = id del huésped y `habitacion`)
  2. **Checkout.** `EliminarPorPrefijoAsync` está listo y probado, pero nada lo dispara en la API: hace falta una credencial de servicio del hotel o su sistema de reservas, distinta del token del huésped (PLAN.md lo imaginaba como tarea programada; el cron solo existe en el plan Premium de SmarterASP)
  3. **Base real y variables del hosting.** La base del hosting es de pruebas y su contraseña se compartió en el chat; rotarla o crear otra al pasar a real. Al cambiar variables del pool, reiniciar el pool. Zona horaria del hotel (`ServicioInfoHotel.Hoy()` usa la hora local del servidor)
  4. **Estancias de ejemplo:** las de la base del hosting (305 y 412) caducan el 2026-10-15 y el 2026-10-14; después `/dev/token` da 404 hasta actualizar las fechas de `estancias`
  5. **Hosting definitivo:** la memoria del pool pasó de 60 MB a ~139 MB con el agente en uso; falta medirla con varios huéspedes a la vez para decidir SmarterASP (Advance/Premium) o Azure App Service
  6. **Vectores:** PostgreSQL con `pgvector` aparte (no está en SmarterASP); candidata Azure Database for PostgreSQL Flexible. `IMemoriaVectorial` sigue sin implementación
  7. **Idea para v2:** sessionId distinto de guestId y varias conversaciones por huésped (ver "Ideas para v2")
- **Diagnóstico en el hosting:** `GET /health` (proceso y memoria) y `GET /health/db` (base y esquema; si falla da solo el tipo de error, el código SQL y los métodos de la pila). El `.env` y las variables del pool son independientes: el hosting no lee el `.env`
- **Si el usuario mueve la carpeta del proyecto:** el `.env` está ignorado por git, así que hay que copiarlo a mano a la raíz nueva (con las tres variables, cada una una sola vez). `.git` y `.claudeignore` viajan con la carpeta. Claude no lee `.env`; solo comprueba su estructura contando caracteres
- **No hay que subir al repositorio:** el archivo de publicación de SmarterASP (contiene credenciales). Ya está en el `.gitignore` (`*.publishsettings`)
- **Visual Studio:** compilar .NET 10 exige una versión reciente; si no la soporta, `dotnet publish -c Release` sobre `src/HotelAgentIA.Api` y subir la carpeta

## Cambio de rumbo (2026-10-09)
Se pasa de ruta de aprendizaje (35 fases, Semantic Kernel primero) a **ruta corta a producción con Agent Framework directo**, sin Semantic Kernel. `PLAN.md` es el plan vigente; el `.docx` queda como referencia (sobre todo el Bloque 5 de producción). Fases A-E de `PLAN.md`: validación, núcleo, API, clientes, producción.

## Cómo trabajamos
- Ciclo por fase: concepto, construcción (desde el cambio a producción Claude escribe el código directamente; el usuario lo revisa en VS Code; en el curso original, fases 0-2, escribía el usuario), depuración. Sin preguntas de comprobación al cerrar
- Al cerrar una fase: proyecto compilando y un commit con mensaje que diga qué se aprendió

## Decisiones vigentes
- PostgreSQL (Npgsql/EF Core) para datos y memoria, sustituye a MySQL; Blob Storage para archivos
- Hosting y costos: es un proyecto piloto para mostrar funcionalidad y potencial. Cuando llegue el momento se decide entre quedarse en SmarterASP (con una base aparte para vectores) o mover todo a Azure; depende de los costos y se evalúan ambos escenarios
- Vectores: sí se usarán (decisión del usuario, 2026-10-12) en una base PostgreSQL aparte con `pgvector`, porque el de SmarterASP no lo tiene. `IMemoriaVectorial` se implementa en Infrastructure con su propia cadena de conexión
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
| IHotelInfoService | IServicioInfoHotel |
| HotelTools | HerramientasHotel |
| ConversationRole, ConversationTurn | RolConversacion, TurnoConversacion |
| MemoryFragment, ScoredMemory | FragmentoMemoria, MemoriaPuntuada |
| IKeyValueMemoryStore | IAlmacenMemoriaClaveValor |
| IVectorMemory | IMemoriaVectorial |
| InMemoryKeyValueStore | AlmacenMemoriaClaveValorEnMemoria |
| MemoryKeys | ClavesMemoria |
| PreferenceExtractor | ExtractorPreferencias |

Para nombres nuevos que el plan da en inglés se sigue la misma convención.

## Estado del código
- `tests/HotelAgentIA.Tests` (xUnit 2.9.3, `Microsoft.AspNetCore.Mvc.Testing`), 91 pruebas en 2 s, sin red, sin base de datos y sin gastar tokens: `Memoria/` (claves, validador, almacén en memoria, conversación), `Hotel/` (`ServicioInfoHotel` con `RepositorioFalso` y `RelojFijo`), `Agente/` (herramientas con el cliente de chat falso: aislamiento por huésped, sin parámetro de huésped, menú por comida, escritura validada) y `Api/` (la API real en memoria con `ApiFactory`: JWT inválido/caducado/otra clave/otra audiencia → 401, aislamiento entre huéspedes, validación 400, SSE, CORS, 422 del filtro de contenido, 502 sin filtrar detalles, límite 429 por huésped). Se comprobó con tres mutaciones (quitar la autorización, fijar el huésped 305 en las herramientas, borrar todo con el prefijo) que las pruebas fallan cuando deben
- `HotelAgentIA.Api` (fase C): `Program.cs` es la raíz de composición (JWT, CORS con `CORS_ORIGINS`, límite de 20 peticiones/min por huésped, DbContext, servicios, `AsistenteHotel` por petición, OpenAPI en Development). `Seguridad/` (`OpcionesJwt`, `EmisorTokens`, `UsuarioExtensiones.HuespedId`), `Chat/` (`PuntosFinalesChat`: `POST /chat` y `POST /chat/stream`, `Contratos`), `ManejadorExcepciones` (JSON inválido → 400). `GET /health/db` (diagnóstico: conecta y lee una tabla; 503 con solo `tipo`, `interno` y `codigoSql`, nunca el mensaje) y `POST /dev/token` solo existe con `DEV_TOKENS=true` y solo emite para huéspedes con estancia activa; la caducidad no pasa de la salida (máx. 12 h). Variables: `JWT_SIGNING_KEY`, `JWT_ISSUER` y `JWT_AUDIENCE` (opcionales), `CORS_ORIGINS`, `POSTGRES_CONNECTION`, `AZURE_OPENAI_*`, `DEV_TOKENS`
- Domain con modelos y contratos (fase 1):
  - `Hotel/`: Restaurante, HorarioApertura, PlatoMenu, Actividad, Huesped, Estancia, IRepositorioInfoHotel
  - `Memoria/`: RolConversacion, TurnoConversacion, FragmentoMemoria, MemoriaPuntuada, IAlmacenMemoriaClaveValor, IMemoriaVectorial
- Application (fase 2), carpeta `Memoria/`: `ClavesMemoria`, `ServicioPreferencias`, `ServicioConversacion`
- Infrastructure (fase 2), carpeta `Memoria/`: `AlmacenMemoriaClaveValorEnMemoria` (`ConcurrentDictionary`; expone `Claves` solo para el banco de pruebas)
- `HotelAgentIA.Console` referencia Application e Infrastructure; `Program.cs` ejecuta el escenario de dos huéspedes (305 y 412), con checkout del 305
- `HotelAgentIA.Api` (fase A, prueba de concepto, no referencia aún a las demás capas): `Program.cs` con `GET /health`, `GET /stream?eventos=N` (SSE, un evento por segundo) y `GET /modelo?q=` (`OpenAIClient` con `Endpoint = <host>/openai/v1/` y la clave, `.GetChatClient(despliegue).AsAIAgent(...)`; 503 si faltan variables). Carga `.env` con DotNetEnv (`NoClobber().TraversePath()`). Paquetes: `Microsoft.Agents.AI.OpenAI` 1.24.0 y `DotNetEnv` 3.2.0. Compila sin advertencias; `/health`, `/stream` y `/modelo` probados en local (`/modelo` responde 200 contra el modelo)
- Infrastructure (fase B), `Chat/` y `Persistencia/`: `ClienteChatFalso`, `HotelDbContext`, entidades, migración `Inicial`, `RepositorioInfoHotel`, `SemillaDatos`, `FabricaHotelDbContext`. La consola corre `PruebaAgenteOffline` y `PruebaBaseDatos` (migra, siembra y lee de Docker)
- Application (fase B): `Hotel/` con `IServicioInfoHotel` y `ServicioInfoHotel` (usa `TimeProvider`), `Agente/` con `HerramientasHotel` (`consultar_restaurantes`, `consultar_menu`, `consultar_actividades`, `consultar_mi_estancia`), `SesionHuesped`, `HistorialConversacionProvider`, `PreferenciasHuespedProvider`; `Memoria/NombresPreferencia`; referencia `Microsoft.Extensions.AI.Abstractions` y `Microsoft.Agents.AI.Abstractions`. La consola corre también `PruebaHerramientas` y `PruebaProveedores`
- Fase B (agente): `Application/Agente/AsistenteHotel` (`ResponderAsync`, `ResponderEnStreamingAsync`) e `InstruccionesAgente` (versión inicial), `HerramientasPreferencias` y `Memoria/ValidadorPreferencias`; `Infrastructure/Chat/FabricaClienteChat`; Application referencia `Microsoft.Agents.AI` e Infrastructure `Microsoft.Agents.AI.OpenAI`. La consola corre también `PruebaPreferencias` y `PruebaModeloReal` (gasta tokens; se omite sin variables)
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
- Fase A: el endpoint es el del proyecto de Foundry (`*.services.ai.azure.com/api/projects/...`); `Azure.AI.OpenAI` 2.1.0 da "API version not supported" contra él. Se usa el cliente `OpenAI` apuntando a `<host>/openai/v1/` con la misma clave
- Fase A: con `NoClobber`, una clave repetida en `.env` con valor vacío gana a la siguiente; cada variable debe aparecer una sola vez
- Fase A: Visual Studio publica un solo proyecto (el del perfil), solo con lo que pertenece a su carpeta; por eso el `.env` de la raíz de la solución no se sube. Las variables del hosting se ponen en el panel (Pool Manager → Environment Variables, a nivel de pool) y no en el `web.config`, que Visual Studio puede regenerar al publicar
- Fase A: ASP.NET Core no lee `.env` por sí solo; se usa DotNetEnv, y las variables de entorno reales tienen prioridad
- Fase A: en un pool compartido con varios sitios .NET solo uno puede ir InProcess; los demás deben ir OutOfProcess (`<AspNetCoreHostingModel>` en el `.csproj`, no a mano en el `web.config`, que se regenera al publicar). Un 500 vacío en todas las rutas, DLL incluida, es fallo de arranque (500.35), no del código
- Fase A: las variables de entorno del pool son por pool y el proceso las lee sin configurar nada en el proyecto; con OutOfProcess `/stream` llega sin buffering a través del proxy de IIS

- Fase B: `ClienteChatFalso` (Infrastructure/Chat) permite probar el agente sin red; `AsAIAgent` sobre `IChatClient` está en el namespace `Microsoft.Extensions.AI`, y la sesión acumula el historial entre turnos
- Fase B: Npgsql arrastra EF Core 10.0.4 y Design trae 10.0.12 (aviso MSB3277): se fijan EF Core y Relational a 10.0.12 en Infrastructure; los tres se actualizan juntos. Npgsql exige `DateTimeOffset` en UTC
- Fase B: el huésped llega a las herramientas por construcción, no por argumento: `HerramientasHotel(servicio, huespedId)` se crea por petición con el huésped del token y se pasa en `ChatClientAgentRunOptions(new ChatOptions { Tools = ... })`; el agente es único y ninguna herramienta tiene parámetro de huésped, así el modelo no puede elegirlo. La documentación de Agent Framework (.NET) ofrece además `AIAgent.CurrentRunContext` (RunOptions/Session) y middleware que inyecta argumentos, pero eso deja el parámetro visible en el esquema que ve el modelo
- Fase B: los proveedores (`ChatHistoryProvider`, `AIContextProvider`) son una sola instancia compartida por todas las sesiones: el huésped no puede ir en un campo. Va en el `StateBag` de una sesión efímera por petición (`SesionHuesped.CrearAsync(agente, huespedId)`), que no se serializa; la memoria durable está en el almacén por huésped. Una sesión creada sin huésped falla a propósito. La documentación usa `StateKey`, pero en 1.24.0 el miembro es `StateKeys` y no hace falta sobrescribirlo
- Fase B: `HistorialConversacionProvider` guarda solo turnos de texto (usuario y asistente), sin llamadas ni resultados de herramientas; filtra los mensajes del usuario por `AgentRequestMessageSourceType.External` para no guardar historial ni contexto añadidos por proveedores. `PreferenciasHuespedProvider` añade las preferencias como instrucciones, en una línea y acotadas a 100 caracteres, presentadas como datos y no como órdenes (mitigación de inyección, no garantía)
- Fase B: en `AIFunctionFactory.Create` un parámetro `string?` sin valor por defecto es obligatorio y la llamada falla con "Function failed" si el modelo lo omite; los opcionales llevan `= null` y `CancellationToken ct = default` va el último
- Fase B: `ClienteChatFalso.ConLlamadaAHerramienta` simula que el modelo llama a una herramienta y comprueba que las herramientas por petición llegan; las respuestas de herramientas son texto en español para ahorrar tokens y los errores previsibles (restaurante inexistente, fecha inválida) se devuelven como texto, no como excepción
- Fase B: las entidades de EF viven en Infrastructure (`Persistencia/Entidades`), separadas de los `record` de Domain; `RepositorioInfoHotel` hace el mapeo. `FabricaHotelDbContext` (lee `POSTGRES_CONNECTION` del `.env`) permite `dotnet ef`; la consola es el proyecto de arranque

- Fase B: la herramienta de escritura es la única excepción a "herramientas de solo lectura": solo toca las preferencias del propio huésped y con valores de conjunto cerrado. Con el modelo real guardó hora de desayuno e idioma de un mensaje y no guardó nada ante un intento de inyección: el filtro de contenido de Azure lo bloqueó antes con HTTP 400 `content_filter` (`ClientResultException`), que la API de la fase C debe capturar y convertir en una respuesta amable. Con idioma=en el modelo aún respondió en español en el streaming: reforzar en las instrucciones (paso 4)
- Fase B: `PlatoMenu` lleva `Comidas` (enum `Comida`: Desayuno, Almuerzo, Cena; lista vacía = cualquier comida), columna `text[]` `Comidas` en `platos_menu` (migración `ComidasEnPlatos`, que también ajusta los platos de ejemplo porque la semilla es idempotente). `consultar_menu(restauranteId, etiquetaDietetica, comida)` filtra por franja y valida la comida. Con el modelo real, cena y almuerzo ya no ofrecen el desayuno buffet. Lección: un fallo del modelo a veces es de datos, no de instrucciones

- Fase C: el huésped sale del claim `sub` del JWT (`MapInboundClaims = false`); probado que el 305 no puede ver datos del 412. `AsistenteHotel` se registra por ámbito (usa el DbContext), no como singleton. El filtro de contenido de Azure se traduce a 422 y otros fallos del modelo a 502, registrando solo el tipo de error, nunca el mensaje. En SSE, si el error ocurre tras empezar, viaja como evento `error`. Los `curl` desde la consola de Windows mandan CP1252: para probar con tildes usar `--data-binary @archivo` en UTF-8. `launchSettings.json` pisa `ASPNETCORE_URLS` en local (puertos 55001/55002)

- Fase C: `IAlmacenMemoriaClaveValor.ActualizarAsync(clave, Func<string?, string>)` hace la lectura-modificación-escritura atómica; en PostgreSQL con `pg_advisory_xact_lock(hashtextextended(clave, 0))` dentro de una transacción, que bloquea incluso si la fila aún no existe y vale entre instancias de la API. `ServicioConversacion` lo usa: 15 turnos simultáneos del mismo huésped no se pierden y los de más de 20 se recortan. Almacén, servicios y `AsistenteHotel` van por ámbito (comparten el DbContext de la petición). `EliminarPorPrefijoAsync` usa `StartsWith`, que EF traduce a `LIKE` escapando `%` y `_` (probado con `pruebaa` frente a `prueba_`). Probado de extremo a extremo: el agente recuerda nombre y hora de desayuno tras reiniciar el proceso de la API, y otro huésped no lo ve
- Fase C: los datos de prueba de la API en tu base local (huésped 305) se quedan guardados; para limpiarlos, `EliminarPorPrefijoAsync(ClavesMemoria.PrefijoHuesped("305"))`

- Fase C (tests): `ApiFactory` fija TODAS las variables de entorno que usa la API antes de arrancar (las de entorno ganan al `.env` por `NoClobber`), porque `Program` carga el `.env` real de la raíz y, sin eso, una prueba podría llegar a la base y al modelo reales. Un fixture compartido (`IClassFixture`) comparte el almacén y el límite de uso entre pruebas: lo que cuenta o inspecciona estado usa su propia `ApiFactory`. xUnit exige un único constructor público en el fixture; el que recibe el modelo falso es `internal`. xUnit v2 necesita `<Using Include="Xunit" />` en el csproj
- Sin cubrir en tests: `AlmacenMemoriaClaveValorPostgres` y `RepositorioInfoHotel` (necesitan PostgreSQL; se prueban a mano en la consola con `PruebaAlmacenPostgres` y `PruebaBaseDatos`), `/dev/token` apagado (`DEV_TOKENS` distinto de `true`) y la caducidad de 12 h

- Fase C (hosting): `/health/db` descubrió que la variable `POSTGRES_CONNECTION` del pool daba `ArgumentException` (cadena ilegible para Npgsql) aunque el valor se veía correcto; se resolvió volviendo a escribir el valor y reiniciando (probable carácter invisible o variable sin recargar). Lecciones: un 500 sin detalles se diagnostica con un endpoint que devuelva solo el tipo de error y el código SQL; el `.env` y las variables del pool son independientes (el hosting no lee el `.env`); el proceso lee las variables al arrancar. El puerto 6432 de site4now parece PgBouncer: no ha dado problemas con el bloqueo consultivo (`ActualizarAsync`), vigilarlo si aparecen fallos intermitentes de memoria
- Fase D: el OpenAPI de .NET no describe las respuestas de `Results.Ok(...)` sin tipar; se añadió `.Produces<T>()`/`.ProducesProblem()` en los endpoints para que el cliente generado tenga `RespuestaChat`, `RespuestaToken` y los errores. Con `erasableSyntaxOnly` (plantilla de Vite) no valen las propiedades en el constructor de TypeScript. `openapi-typescript` 7 no admite TypeScript 7: fijar `~6.0`. `curl` con tildes desde Windows da 400 (usar `--data-binary @archivo`). La sesión del web va en `sessionStorage` (equipo compartido) y se limpia al caducar o recibir 401
- Fase C (hosting): la base del hosting es de pruebas; la real se configurará al publicar. La contraseña de pruebas se compartió en el chat: no repetirlo con la real

## Limitaciones conocidas
- La semilla fija las estancias relativas al momento de sembrar (305 hasta 2026-10-15, 412 hasta 2026-10-14): pasada esa fecha `/dev/token` responde 404 y el agente dice que no hay estancia activa. Para renovarlas hay que actualizar las fechas de `estancias` en la base (la semilla no vuelve a correr porque ya hay restaurantes)
- `/dev/token` es provisional: en producción lo sustituye el login real del hotel (Pregunta abierta: cómo se autentica el huésped)
- El almacén en memoria (`AlmacenMemoriaClaveValorEnMemoria`) solo sirve para el banco de pruebas: pierde todo al reiniciar. La API usa PostgreSQL
- Nada borra aún la memoria de un huésped al hacer checkout en la API (`EliminarPorPrefijoAsync` existe y está probado, falta el disparador: el hotel/PMS necesita una credencial de servicio distinta del token del huésped)
- `ActualizarAsync` en el almacén en memoria usa `AddOrUpdate`, que puede reintentar la función bajo contienda; solo importa en pruebas

## Experimento pendiente (fase 1, opcional)
- Añadir `using HotelAgentIA.Application;` dentro de un archivo de Domain, compilar y anotar el error; luego quitarlo

## Para fases siguientes
- Zona horaria del hotel: `ServicioInfoHotel.Hoy()` usa la hora local del servidor (`TimeProvider`); en el hosting puede no ser la del hotel. Configurar una zona horaria del hotel
- Fase 5: decidir dónde vive la clave del wifi (no va en `Estancia`)
- Fase 5: representar horarios que cierran pasada la medianoche (`HorarioApertura` con `TimeOnly`)

## Ideas para v2
- Conversaciones múltiples por huésped: añadir un `conversacionId` generado por nosotros dentro del prefijo del huésped (`huesped:{id}:conversacion:{conversacionId}`). El checkout sigue borrando todo con un solo prefijo y se puede borrar un chat concreto con `EliminarAsync`
- Conservar preferencias para que el hotel decida: guardar solo estadísticas agregadas y anónimas en tablas aparte, y borrar lo individual en el checkout. Requiere consentimiento del huésped y revisar la normativa de privacidad aplicable

## Siguiente: fase D (clientes)
- Ver "Para retomar". `ExtractorPreferencias` se descartó: las preferencias las escribe el modelo con la herramienta `guardar_preferencia`, validada contra un conjunto cerrado

## Dudas
- (ninguna abierta)

## Cómo ejecutar
- Preparar una base nueva (migra y siembra, sin más pruebas): `dotnet run --project src/HotelAgentIA.Console -- --solo-bd`. Usa `POSTGRES_CONNECTION` del `.env`; si el `.env` apunta al hosting, la consola completa también escribiría allí (semilla, claves `prueba*`) y `PruebaModeloReal` gasta tokens: usa `--solo-bd` o apunta el `.env` a la base local
- Compilar: `dotnet build HotelAgent.slnx`
- Consola: `dotnet run --project src/HotelAgentIA.Console`
- API de prueba: `dotnet run --project src/HotelAgentIA.Api` (necesita `.env` en la raíz para `/modelo`)
- PostgreSQL local: contenedor Docker `hotelagent-pg` (`docker start hotelagent-pg`); `POSTGRES_CONNECTION` en el `.env`
- Nueva migración: `dotnet ef migrations add <Nombre> --project src/HotelAgentIA.Infrastructure --startup-project src/HotelAgentIA.Console --output-dir Persistencia/Migraciones`
