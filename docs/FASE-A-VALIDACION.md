# Fase A: validación (2026-10-09)

Hallazgos de la documentación oficial y de las páginas de SmarterASP. Lo no confirmado está marcado y es trabajo pendiente antes de depender de ello.

## 1. Agent Framework (.NET)

**Versiones (NuGet, consultado el 2026-10-09)**
| Paquete | Estado |
|---|---|
| `Microsoft.Agents.AI` | 1.24.0 estable (2026-10-07); targets .NET 8/9/10 |
| `Microsoft.Agents.AI.Foundry` | 1.5.0 estable (2026-05-08); prerelease 1.24.0-preview.261006.1. Net10 depende de Azure.AI.Projects 2.0.0, Azure.Identity 1.21.0, Microsoft.Extensions.AI 10.5.1 |
| `Microsoft.Agents.AI.Hosting` (+ `.OpenAI`, `.A2A.AspNetCore`, `.AspNetCore`) | **Prerelease**; la documentación pide instalar con `--prerelease` y revisar notas antes de actualizar en producción |

El plan Word decía "Agent Framework 1.0 estable desde abril de 2026": se ha superado (ya va por 1.24). Fijar versiones exactas en `Directory.Packages.props` y no usar rangos.

**API verificada en la documentación**
- Agente contra Foundry: `new AIProjectClient(new Uri(endpoint), credential).AsAIAgent(model:, instructions:, tools:)` y `await agent.RunAsync(texto, session)`.
- Herramientas: `AIFunctionFactory.Create(metodo)`, con `[Description]` en método y parámetros. Compatible con Foundry (function tools: sí). Existe aprobación de herramientas (*Tool Approval*) con humano en el bucle.
- Sesiones: `await agent.CreateSessionAsync()`, `agent.SerializeSession(session)` y `await agent.DeserializeSessionAsync(json)`. `AgentSession` lleva `StateBag`.
- Historial: clase base `Microsoft.Agents.AI.ChatHistoryProvider` (sobrescribir `ProvideChatHistoryAsync` y `StoreChatHistoryAsync`), `ProviderSessionState<T>` para estado por sesión, `ChatClientAgentOptions.ChatHistoryProvider`, `InMemoryChatHistoryProvider` con `MessageCountingChatReducer(20)`.
- Persistencia en hosting: `AgentSessionStore` (Save/Get/Delete) con aislamiento por usuario (`UseClaimsBasedAgentIsolation`); el framework **no** incluye un almacén durable: hay que implementarlo (PostgreSQL).
- Credenciales: la documentación advierte contra `DefaultAzureCredential` en producción; usar `ManagedIdentityCredential` o una credencial concreta.

**Implicaciones para nuestro diseño**
- `ServicioConversacion` y `AlmacenMemoriaClaveValor` no desaparecen: el historial pasa a un `ChatHistoryProvider` propio sobre PostgreSQL y las preferencias a un proveedor de contexto. Conservamos `ClavesMemoria` y el aislamiento por prefijo.
- Advertencia oficial: el id de sesión del cliente **no** prueba propiedad. Mapear el id de sesión del cliente a nuestro id interno y verificar siempre que pertenece al huésped del token (encaja con la decisión "el huésped sale del token").

**No verificado todavía (revisar al construir la fase B)**
- Nombre exacto de `AIContextProvider` y sus métodos en .NET (la página de memoria devolvió 404).
- Cómo pasar contexto de ejecución (guestId) a una herramienta en .NET. En Python existe `FunctionInvocationContext`; en .NET no lo confirmé. Plan: crear las herramientas como closures o métodos de una clase con el guestId ya resuelto del token, construidas por petición (el modelo nunca lo ve).
- `RunStreamingAsync` y su forma para SSE.
- Modelo disponible en nuestro proyecto de Foundry (la documentación usa `gpt-5.4-mini` en ejemplos).

## 2. SmarterASP.NET

Fuente: https://www.smarterasp.net/hosting_plans (precios mensuales).

| Plan | Precio | PostgreSQL | MySQL | Memoria del pool |
|---|---|---|---|---|
| Basic | 2,95 USD | 1 GB | 1 GB | 256 MB |
| Advance | 4,95 USD | 3 GB | 3 GB | 512 MB |
| Premium | 7,95 USD | 10 GB | 10 GB | 3 GB |

Prueba gratuita de 60 días disponible (pool de 800 MB).

**Confirmado:** .NET 10.x, PostgreSQL incluido en todos los planes (resuelve la duda inicial), MySQL 8, SQL Server hasta 2025, *ASP.NET WebSockets* incluido, tareas programadas (cron) solo en Premium.

**No confirmado / riesgos** (probar en la prueba gratuita antes de decidir):
1. **SSE sin buffering**: la página no lo menciona. Probar un endpoint de streaming real.
2. **Reciclado del pool y falta de "always on"**: en IIS compartido el proceso se duerme y se recicla; cualquier estado en memoria (el almacén actual, sesiones) se pierde. Por eso toda memoria debe ir a PostgreSQL, que ya es la decisión.
3. **Límite de memoria 256-512 MB** en Basic/Advance: ajustado para ASP.NET Core + Agent Framework. Probar con 512 MB (Advance) como mínimo; Premium da margen.
4. **Salida al modelo** (HTTPS a `*.openai.azure.com`) y a Entra ID/Managed Identity: **la identidad administrada no existe fuera de Azure**. Se autentica con la clave de API de Azure OpenAI (variables `AZURE_OPENAI_*` del hosting, no en el repositorio); un service principal es la alternativa si se quiere Entra ID.
5. **PostgreSQL**: versión y extensiones. `pgvector` no está confirmado; si no está, los vectores se resuelven con otra pieza o se omiten en el MVP (los datos del hotel van por herramientas, no por vectores).
6. **Tareas en segundo plano**: el borrado de memoria al checkout necesita un cron; solo Premium lo trae. Alternativa: endpoint protegido llamado por un cron externo (p. ej. GitHub Actions programado).
7. **Despliegue**: Web Deploy/FTP sin Key Vault; secretos como variables o `appsettings` fuera del repositorio.

## 3. Decisión provisional de hosting

- Probar el plan de prueba de 60 días con un **endpoint de streaming de prueba y una llamada al modelo**.
- Si SSE, memoria y salida al modelo funcionan: SmarterASP Advance o Premium para API + PostgreSQL, y frontend web estático en el mismo sitio.
- Si falla alguno: **Azure App Service** (Linux, B1) para la API; PostgreSQL gestionado (Azure Database for PostgreSQL Flexible) o el de SmarterASP. Con Azure se gana identidad administrada, Key Vault y webjobs/timer.
- Una prueba de concepto de una hora decide esto; no hace falta más investigación.

## 4. Siguientes pasos

1. Crear el proyecto de prueba `HotelAgentIA.Api` (mínimo) con un endpoint `GET /stream` y otro `GET /modelo` que llame al modelo; publicarlo en la prueba de 60 días de SmarterASP.
2. Decidir hosting con ese resultado.
3. Empezar la fase B: Npgsql/EF Core en Infrastructure y el esqueleto del agente con `IChatClient` de prueba (modo offline).

## 5. Publicación en SmarterASP (decisión de procedimiento)

- Para la prueba de 60 días: perfil de Web Deploy descargado del panel e importado en Visual Studio, publicando solo `HotelAgentIA.Api`. Alternativa: `dotnet publish -c Release` y subir la carpeta. Conectar GitHub o automatizar con GitHub Actions queda para la fase de despliegue; no se verificó si SmarterASP integra GitHub directamente.
- Variables en el hosting: Control panel → Advance → Pool Manager → Actions → Environment Variables ([guía](https://www.smarterasp.net/support/kb/a2156/how-to-set-environment-variable-for-your-account.aspx)). Se aplican a todo el pool. La alternativa `web.config` ([guía](https://www.smarterasp.net/support/kb/a1869/how-to-config-aspnetcore-environment-for-asp_net-core-application.aspx)) se descarta: puede regenerarse al publicar y deja la clave en un archivo de texto. Las guías son antiguas; confirmar en el panel actual.
- El archivo de publicación contiene credenciales: no va al repositorio.

## 6. Resultados en el hosting (2026-10-12)

Sitio de prueba: https://davidroblero-001-site2.ftempurl.com/ (prueba de 60 días, pool compartido con otros 3 sitios .NET).

| Prueba | Resultado |
|---|---|
| `/health`: versión de .NET y memoria | 200; .NET 10.0.7; 48 MB en reposo |
| `/stream`: ¿llegan los eventos uno a uno? | Sí: 5 eventos con ~1 s entre cada uno, sin buffering (con OutOfProcess, IIS hace de proxy hacia Kestrel) |
| `/modelo`: ¿responde 200 con las variables del pool? | Sí: 200 y `{"respuesta":"OK"}` |
| Memoria del pool con el plan elegido | Pendiente: medir bajo carga con el agente completo |
| `pgvector` disponible en PostgreSQL | Pendiente: se prueba al empezar la fase B |
| Decisión de hosting | SmarterASP sigue siendo viable (SSE, salida al modelo y .NET 10 confirmados). Se cierra tras medir memoria y `pgvector`; plan B: Azure App Service |

### Causa del HTTP 500 inicial
- El sitio se publicó con el modelo de hospedaje **InProcess**. El pool solo admite **una app InProcess**, y otro sitio ya la ocupaba: el módulo de IIS falla al arrancar (error 500.35) y devuelve 500 con cuerpo vacío en todas las rutas, incluida la DLL.
- Al corregirlo a mano en el `web.config` del servidor se escribió mal el valor (`outprocess`); con `outofprocess` arrancó.
- No eran la versión de .NET, Server GC, los archivos ni las variables de entorno.
- Solución permanente: `<AspNetCoreHostingModel>OutOfProcess</AspNetCoreHostingModel>` en `HotelAgentIA.Api.csproj`, para que el `web.config` generado al publicar ya lleve `outofprocess` y no se pierda el arreglo.

### Lecciones
- En un pool compartido con varios sitios .NET, todos deben ir en OutOfProcess salvo uno.
- Un 500 vacío en todas las rutas, DLL incluida, indica fallo al arrancar el proceso, no un error del código. El log de arranque está en `logs\stdout_*.log` con `stdoutLogEnabled="true"`.
- Las variables de entorno del pool las lee el proceso sin configuración en el proyecto; son por pool, no por sitio.
