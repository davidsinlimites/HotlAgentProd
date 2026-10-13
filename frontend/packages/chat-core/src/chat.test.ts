import { describe, expect, it } from 'vitest'
import { crearAlmacenChat } from './almacenChat'
import { leerEventosSse } from './sse'
import { ErrorChat, transmitirChat } from './transmitir'

function flujo(...trozos: string[]) {
  const codificador = new TextEncoder()
  return new ReadableStream<Uint8Array>({
    start(c) {
      trozos.forEach((t) => c.enqueue(codificador.encode(t)))
      c.close()
    },
  })
}

async function todos(cuerpo: ReadableStream<Uint8Array>) {
  const salida = []
  for await (const e of leerEventosSse(cuerpo)) salida.push(e)
  return salida
}

const respuestaSse = (...trozos: string[]) =>
  new Response(flujo(...trozos), { headers: { 'Content-Type': 'text/event-stream' } })

describe('leerEventosSse', () => {
  it('lee eventos de datos y fin', async () => {
    expect(await todos(flujo('data: "Hola"\n\ndata: " mundo"\n\nevent: fin\ndata: {}\n\n'))).toEqual([
      { evento: 'message', datos: '"Hola"' },
      { evento: 'message', datos: '" mundo"' },
      { evento: 'fin', datos: '{}' },
    ])
  })

  it('une eventos partidos entre trozos de red y acepta CRLF', async () => {
    expect(await todos(flujo('data: "Ho', 'la"\r\n', '\r\nevent: fin\r\ndata: {}\r\n\r\n'))).toEqual([
      { evento: 'message', datos: '"Hola"' },
      { evento: 'fin', datos: '{}' },
    ])
  })

  it('decodifica tildes partidas entre trozos', async () => {
    const bytes = new TextEncoder().encode('data: "ñ"\n\n')
    const cuerpo = new ReadableStream<Uint8Array>({
      start(c) {
        c.enqueue(bytes.slice(0, 8)) // corta en mitad de la ñ
        c.enqueue(bytes.slice(8))
        c.close()
      },
    })
    expect(await todos(cuerpo)).toEqual([{ evento: 'message', datos: '"ñ"' }])
  })
})

describe('transmitirChat', () => {
  const base = { baseUrl: 'http://api', token: 't', mensaje: 'hola' }

  it('entrega los trozos y envia el token', async () => {
    const trozos: string[] = []
    let cabecera = ''
    await transmitirChat({
      ...base,
      alTrozo: (t) => trozos.push(t),
      fetchImpl: async (_url, init) => {
        cabecera = new Headers(init?.headers).get('Authorization') ?? ''
        return respuestaSse('data: "A"\n\ndata: "B"\n\nevent: fin\ndata: {}\n\n')
      },
    })
    expect(trozos).toEqual(['A', 'B'])
    expect(cabecera).toBe('Bearer t')
  })

  it.each([
    [401, 'caducado'],
    [429, 'rapido'],
  ])('traduce el estado %i', async (estado, fragmento) => {
    const intento = transmitirChat({ ...base, alTrozo: () => {}, fetchImpl: async () => new Response('', { status: estado }) })
    await expect(intento).rejects.toMatchObject({ estado, message: expect.stringContaining(fragmento) })
  })

  it('usa el detalle del problema JSON en 422', async () => {
    const cuerpo = JSON.stringify({ detail: 'No puedo ayudar con ese mensaje.' })
    const intento = transmitirChat({ ...base, alTrozo: () => {}, fetchImpl: async () => new Response(cuerpo, { status: 422 }) })
    await expect(intento).rejects.toMatchObject({ estado: 422, message: 'No puedo ayudar con ese mensaje.' })
  })

  it('el evento error a mitad del stream lanza ErrorChat', async () => {
    const intento = transmitirChat({
      ...base,
      alTrozo: () => {},
      fetchImpl: async () => respuestaSse('data: "A"\n\nevent: error\ndata: "Fallo"\n\n'),
    })
    await expect(intento).rejects.toBeInstanceOf(ErrorChat)
  })

  it('un stream sin fin es una interrupcion', async () => {
    const intento = transmitirChat({ ...base, alTrozo: () => {}, fetchImpl: async () => respuestaSse('data: "A"\n\n') })
    await expect(intento).rejects.toMatchObject({ message: 'La respuesta se interrumpio.' })
  })
})

describe('almacenChat', () => {
  it('acumula la respuesta y avisa si la sesion caduco', async () => {
    const ok = crearAlmacenChat({
      baseUrl: '',
      obtenerToken: () => 't',
      fetchImpl: async () => respuestaSse('data: "Ho"\n\ndata: "la"\n\nevent: fin\ndata: {}\n\n'),
    })
    await ok.getState().enviar(' hola ')
    expect(ok.getState().mensajes.map((m) => [m.rol, m.texto])).toEqual([
      ['usuario', 'hola'],
      ['asistente', 'Hola'],
    ])

    let caducada = false
    const caido = crearAlmacenChat({
      baseUrl: '',
      obtenerToken: () => 't',
      alSesionCaducada: () => (caducada = true),
      fetchImpl: async () => new Response('', { status: 401 }),
    })
    await caido.getState().enviar('hola')
    expect(caducada).toBe(true)
    expect(caido.getState().mensajes[1]).toMatchObject({ error: true })
    expect(caido.getState().enCurso).toBe(false)
  })

  it('no envia sin token', async () => {
    const almacen = crearAlmacenChat({ baseUrl: '', obtenerToken: () => null })
    await almacen.getState().enviar('hola')
    expect(almacen.getState().mensajes).toEqual([])
  })
})
