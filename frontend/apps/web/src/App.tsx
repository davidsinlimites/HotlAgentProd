import { Chat } from './Chat'
import { Login } from './Login'
import { useSesion } from './sesion'

export default function App() {
  const token = useSesion((s) => s.token)
  return token ? <Chat /> : <Login />
}
