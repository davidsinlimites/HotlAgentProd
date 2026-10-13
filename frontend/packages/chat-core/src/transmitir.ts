import { leerEventosSse } from './sse'

export class ErrorChat extends Error {
  /** Estado HTTP previo al stream (400, 401, 422, 429, 502) o 0 si fue de red o a mitad del stream. */
  readonly estado: number

  constructor(mensaje: string, estado: number) {
    super(mensaje)
    this.name = 'ErrorChat'
    this.estado = estado
  }
}

export interface OpcionesTransmision {
  baseUrl: string
  token: string
  mensaje: string
  alTrozo: (texto: string) => void
  signal?: AbortSignal
  /** Inyectable para pruebas o para el fetch con streaming de React Native. */
  fetchImpl?: typeof fetch
}

/** POST /chat/stream. Resuelve al llegar `event: fin`; lanza ErrorChat en cualquier fallo. */
export async function transmitirChat({ baseUrl, token, mensaje, alTrozo, signal, fetchImpl = fetch }: OpcionesTransmision) {
  let respuesta: Response
  try {
    respuesta = await fetchImpl(`${baseUrl}/chat/stream`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ mensaje }),
      signal,
    })
  } catch (e) {
    if (esAborto(e)) throw e
    throw new ErrorChat('No se pudo conectar con el hotel. Revisa tu conexion.', 0)
  }

  if (!respuesta.ok || !respuesta.body) {
    throw new ErrorChat(await detalleDeProblema(respuesta), respuesta.status)
  }

  for await (const { evento, datos } of leerEventosSse(respuesta.body)) {
    if (evento === 'fin') return
    if (evento === 'error') throw new ErrorChat(JSON.parse(datos) as string, 0)
    if (evento === 'message') alTrozo(JSON.parse(datos) as string)
  }
  throw new ErrorChat('La respuesta se interrumpio.', 0)
}

/** Sin `instanceof DOMException`: Hermes (React Native) puede no tenerlo. */
export function esAborto(e: unknown): boolean {
  return typeof e === 'object' && e !== null && (e as { name?: unknown }).name === 'AbortError'
}

async function detalleDeProblema(respuesta: Response): Promise<string> {
  if (respuesta.status === 401) return 'Tu sesion ha caducado. Vuelve a entrar.'
  if (respuesta.status === 429) return 'Estas escribiendo muy rapido. Espera un momento.'
  try {
    const problema = (await respuesta.json()) as { detail?: string }
    if (problema.detail) return problema.detail
  } catch {
    // cuerpo no JSON: se usa el mensaje generico
  }
  return 'No pude responder ahora mismo. Intentalo de nuevo.'
}
