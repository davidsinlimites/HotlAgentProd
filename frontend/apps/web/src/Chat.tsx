import { useEffect, useRef, useState, type FormEvent } from 'react'
import { almacenChat, useChat } from './api'
import { useSesion } from './sesion'

export function Chat() {
  const mensajes = useChat((s) => s.mensajes)
  const enCurso = useChat((s) => s.enCurso)
  const cerrar = useSesion((s) => s.cerrar)
  const [texto, setTexto] = useState('')
  const fin = useRef<HTMLDivElement>(null)

  useEffect(() => {
    fin.current?.scrollIntoView({ behavior: 'smooth' })
  }, [mensajes])

  function enviar(e: FormEvent) {
    e.preventDefault()
    const t = texto
    setTexto('')
    void almacenChat.getState().enviar(t)
  }

  function salir() {
    almacenChat.getState().limpiar()
    cerrar()
  }

  return (
    <div className="mx-auto flex h-full max-w-2xl flex-col bg-white">
      <header className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
        <h1 className="font-semibold text-slate-900">Asistente del hotel</h1>
        <button onClick={salir} className="text-sm text-slate-600 underline">Salir</button>
      </header>

      <div className="flex-1 space-y-3 overflow-y-auto p-4" aria-live="polite">
        {mensajes.length === 0 && (
          <p className="text-slate-500">Pregúntame por los restaurantes, el menú, las actividades o tu estancia.</p>
        )}
        {mensajes.map((m) => (
          <div key={m.id} className={m.rol === 'usuario' ? 'flex justify-end' : 'flex justify-start'}>
            <p
              className={
                'max-w-[85%] whitespace-pre-wrap rounded-2xl px-4 py-2 ' +
                (m.rol === 'usuario'
                  ? 'bg-slate-900 text-white'
                  : m.error
                    ? 'bg-red-50 text-red-800'
                    : 'bg-slate-100 text-slate-900')
              }
            >
              {m.texto || (enCurso ? '…' : '')}
            </p>
          </div>
        ))}
        <div ref={fin} />
      </div>

      <form onSubmit={enviar} className="flex gap-2 border-t border-slate-200 p-3">
        <input
          value={texto}
          onChange={(e) => setTexto(e.target.value)}
          maxLength={2000}
          placeholder="Escribe tu mensaje"
          className="flex-1 rounded-lg border border-slate-300 px-3 py-2"
          autoFocus
        />
        {enCurso ? (
          <button type="button" onClick={() => almacenChat.getState().cancelar()} className="rounded-lg border border-slate-300 px-4 py-2">
            Detener
          </button>
        ) : (
          <button disabled={!texto.trim()} className="rounded-lg bg-slate-900 px-4 py-2 font-medium text-white disabled:opacity-50">
            Enviar
          </button>
        )}
      </form>
    </div>
  )
}
