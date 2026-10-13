export interface EventoSse {
  /** 'message' cuando el servidor no pone `event:`. */
  evento: string
  datos: string
}

/**
 * Convierte un flujo de bytes SSE en eventos. Soporta saltos \n y \r\n y
 * eventos partidos entre trozos de red.
 */
export async function* leerEventosSse(cuerpo: ReadableStream<Uint8Array>): AsyncGenerator<EventoSse> {
  const lector = cuerpo.getReader()
  const decodificador = new TextDecoder()
  let pendiente = ''

  try {
    while (true) {
      const { done, value } = await lector.read()
      if (done) break
      pendiente += decodificador.decode(value, { stream: true }).replace(/\r\n/g, '\n')

      let corte: number
      while ((corte = pendiente.indexOf('\n\n')) !== -1) {
        const bloque = pendiente.slice(0, corte)
        pendiente = pendiente.slice(corte + 2)
        const evento = analizarBloque(bloque)
        if (evento) yield evento
      }
    }
    const resto = analizarBloque(pendiente.trim())
    if (resto) yield resto
  } finally {
    lector.releaseLock()
  }
}

function analizarBloque(bloque: string): EventoSse | null {
  let evento = 'message'
  const datos: string[] = []
  for (const linea of bloque.split('\n')) {
    if (linea.startsWith('event:')) evento = linea.slice(6).trim()
    else if (linea.startsWith('data:')) datos.push(linea.slice(5).replace(/^ /, ''))
  }
  return datos.length === 0 && evento === 'message' ? null : { evento, datos: datos.join('\n') }
}
