import { crearClienteApi } from '@hotelagent/api-client'
import { crearAlmacenChat } from '@hotelagent/chat-core'
import { fetch as fetchConStreaming } from 'expo/fetch'
import { Platform } from 'react-native'
import { useStore } from 'zustand'
import { useSesion } from './sesion'

// El emulador de Android ve el PC en 10.0.2.2; en un movil fisico o en produccion usa EXPO_PUBLIC_API_URL
const porDefecto = Platform.OS === 'android' ? 'http://10.0.2.2:55002' : 'http://localhost:55002'
export const baseUrl = process.env.EXPO_PUBLIC_API_URL ?? porDefecto

export const api = crearClienteApi({ baseUrl, obtenerToken: () => useSesion.getState().token })

export const almacenChat = crearAlmacenChat({
  baseUrl,
  obtenerToken: () => useSesion.getState().token,
  alSesionCaducada: () => useSesion.getState().cerrar(),
  // El fetch de React Native no expone response.body como flujo; el de expo si
  fetchImpl: fetchConStreaming as unknown as typeof fetch,
})

export const useChat = <T,>(selector: (s: ReturnType<typeof almacenChat.getState>) => T) => useStore(almacenChat, selector)
