import { crearClienteApi } from '@hotelagent/api-client'
import { crearAlmacenChat } from '@hotelagent/chat-core'
import { useStore } from 'zustand'
import { useSesion } from './sesion'

export const baseUrl = (import.meta.env.VITE_API_URL as string | undefined) ?? ''

export const api = crearClienteApi({ baseUrl, obtenerToken: () => useSesion.getState().token })

export const almacenChat = crearAlmacenChat({
  baseUrl,
  obtenerToken: () => useSesion.getState().token,
  alSesionCaducada: () => useSesion.getState().cerrar(),
})

export const useChat = <T,>(selector: (s: ReturnType<typeof almacenChat.getState>) => T) => useStore(almacenChat, selector)
