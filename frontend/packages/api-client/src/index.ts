import createClient, { type Middleware } from 'openapi-fetch'
import type { components, paths } from './esquema'

export type { components, paths }
export type PeticionChat = components['schemas']['PeticionChat']
export type RespuestaChat = components['schemas']['RespuestaChat']
export type RespuestaToken = components['schemas']['RespuestaToken']

export interface OpcionesCliente {
  /** Vacio = mismo origen (en desarrollo, el proxy de Vite). */
  baseUrl: string
  /** Se llama en cada peticion; el token sale del almacen de sesion del cliente. */
  obtenerToken: () => string | null
}

export function crearClienteApi({ baseUrl, obtenerToken }: OpcionesCliente) {
  const cliente = createClient<paths>({ baseUrl })
  const autorizacion: Middleware = {
    onRequest({ request }) {
      const token = obtenerToken()
      if (token) request.headers.set('Authorization', `Bearer ${token}`)
      return request
    },
  }
  cliente.use(autorizacion)
  return cliente
}

export type ClienteApi = ReturnType<typeof crearClienteApi>
