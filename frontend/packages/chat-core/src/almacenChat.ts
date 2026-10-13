import { createStore } from 'zustand/vanilla'
import { ErrorChat, esAborto, transmitirChat } from './transmitir'

export interface MensajeChat {
  id: string
  rol: 'usuario' | 'asistente'
  texto: string
  error?: boolean
}

export interface EstadoChat {
  mensajes: MensajeChat[]
  enCurso: boolean
  enviar: (texto: string) => Promise<void>
  cancelar: () => void
  limpiar: () => void
}

export interface DependenciasChat {
  baseUrl: string
  obtenerToken: () => string | null
  /** Se llama si la API responde 401, para que la app vuelva al login. */
  alSesionCaducada?: () => void
  fetchImpl?: typeof fetch
}

let contador = 0
// Los ids solo sirven de clave de lista en la UI: no hace falta crypto (no existe en todos los motores)
const nuevoId = () => `${Date.now().toString(36)}-${(contador++).toString(36)}`

/** Store vanilla: la web lo envuelve con `useStore` de zustand y el movil igual. */
export function crearAlmacenChat(deps: DependenciasChat) {
  let control: AbortController | null = null

  return createStore<EstadoChat>((set, get) => {
    const actualizarUltimo = (cambio: (m: MensajeChat) => MensajeChat) =>
      set((s) => ({ mensajes: s.mensajes.map((m, i) => (i === s.mensajes.length - 1 ? cambio(m) : m)) }))

    return {
      mensajes: [],
      enCurso: false,

      async enviar(texto) {
        const limpio = texto.trim()
        const token = deps.obtenerToken()
        if (!limpio || get().enCurso || !token) return

        control = new AbortController()
        set((s) => ({
          enCurso: true,
          mensajes: [
            ...s.mensajes,
            { id: nuevoId(), rol: 'usuario', texto: limpio },
            { id: nuevoId(), rol: 'asistente', texto: '' },
          ],
        }))

        try {
          await transmitirChat({
            baseUrl: deps.baseUrl,
            token,
            mensaje: limpio,
            signal: control.signal,
            fetchImpl: deps.fetchImpl,
            alTrozo: (trozo) => actualizarUltimo((m) => ({ ...m, texto: m.texto + trozo })),
          })
        } catch (e) {
          if (esAborto(e)) return
          const error = e instanceof ErrorChat ? e : new ErrorChat('Algo salio mal.', 0)
          if (error.estado === 401) deps.alSesionCaducada?.()
          // Si ya habia texto parcial se conserva y se marca el fallo
          actualizarUltimo((m) => ({ ...m, texto: m.texto ? `${m.texto}\n\n${error.message}` : error.message, error: true }))
        } finally {
          control = null
          set({ enCurso: false })
        }
      },

      cancelar() {
        control?.abort()
      },

      limpiar() {
        control?.abort()
        set({ mensajes: [], enCurso: false })
      },
    }
  })
}

export type AlmacenChat = ReturnType<typeof crearAlmacenChat>
