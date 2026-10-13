import { useState, type FormEvent } from 'react'
import { api } from './api'
import { useSesion } from './sesion'

// Provisional: POST /dev/token no pide contrasena y solo existe con DEV_TOKENS=true (demo con datos ficticios).
// El login real del hotel esta pendiente de decision (ver ESTADO.md).
export function Login() {
  const guardar = useSesion((s) => s.guardar)
  const [huespedId, setHuespedId] = useState('305')
  const [error, setError] = useState<string | null>(null)
  const [cargando, setCargando] = useState(false)

  async function entrar(e: FormEvent) {
    e.preventDefault()
    setCargando(true)
    setError(null)
    try {
      const { data, response } = await api.POST('/dev/token', { body: { huespedId: huespedId.trim() } })
      if (data) guardar(data.token, data.caduca)
      else setError(response.status === 404 ? 'No hay una estancia activa para ese huésped.' : 'No se pudo iniciar sesión.')
    } catch {
      setError('No se pudo conectar con el hotel.')
    } finally {
      setCargando(false)
    }
  }

  return (
    <main className="flex h-full items-center justify-center bg-slate-100 p-4">
      <form onSubmit={entrar} className="w-full max-w-sm space-y-4 rounded-xl bg-white p-6 shadow">
        <h1 className="text-xl font-semibold text-slate-900">Asistente del hotel</h1>
        <p className="text-sm text-slate-600">Modo demostración: indica el número de huésped.</p>
        <label className="block text-sm font-medium text-slate-700">
          Huésped
          <input
            value={huespedId}
            onChange={(e) => setHuespedId(e.target.value)}
            className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2"
            autoFocus
          />
        </label>
        {error && <p role="alert" className="text-sm text-red-700">{error}</p>}
        <button
          disabled={cargando || !huespedId.trim()}
          className="w-full rounded-lg bg-slate-900 px-3 py-2 font-medium text-white disabled:opacity-50"
        >
          {cargando ? 'Entrando…' : 'Entrar'}
        </button>
      </form>
    </main>
  )
}
