import { useState } from 'react'
import { ActivityIndicator, KeyboardAvoidingView, Platform, Pressable, StyleSheet, Text, TextInput, View } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'
import { api } from '@/lib/api'
import { useSesion } from '@/lib/sesion'

// Provisional: POST /dev/token no pide contrasena y solo existe con DEV_TOKENS=true (demo con datos ficticios).
// El login real del hotel esta pendiente de decision (ver ESTADO.md).
export default function Login() {
  const guardar = useSesion((s) => s.guardar)
  const [huespedId, setHuespedId] = useState('305')
  const [error, setError] = useState<string | null>(null)
  const [cargando, setCargando] = useState(false)

  async function entrar() {
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
    <SafeAreaView style={estilos.pantalla}>
      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={estilos.centro}>
        <View style={estilos.tarjeta}>
          <Text style={estilos.titulo}>Asistente del hotel</Text>
          <Text style={estilos.ayuda}>Modo demostración: indica el número de huésped.</Text>
          <TextInput
            value={huespedId}
            onChangeText={setHuespedId}
            style={estilos.campo}
            accessibilityLabel="Huésped"
            autoCapitalize="none"
            autoCorrect={false}
          />
          {error && <Text style={estilos.error}>{error}</Text>}
          <Pressable
            onPress={entrar}
            disabled={cargando || !huespedId.trim()}
            style={[estilos.boton, (cargando || !huespedId.trim()) && estilos.deshabilitado]}
            accessibilityRole="button"
          >
            {cargando ? <ActivityIndicator color="#fff" /> : <Text style={estilos.textoBoton}>Entrar</Text>}
          </Pressable>
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  )
}

const estilos = StyleSheet.create({
  pantalla: { flex: 1, backgroundColor: '#f1f5f9' },
  centro: { flex: 1, justifyContent: 'center', padding: 16 },
  tarjeta: { backgroundColor: '#fff', borderRadius: 16, padding: 24, gap: 12 },
  titulo: { fontSize: 20, fontWeight: '600', color: '#0f172a' },
  ayuda: { color: '#475569' },
  campo: { borderWidth: 1, borderColor: '#cbd5e1', borderRadius: 8, paddingHorizontal: 12, paddingVertical: 10, fontSize: 16 },
  error: { color: '#b91c1c' },
  boton: { backgroundColor: '#0f172a', borderRadius: 8, paddingVertical: 12, alignItems: 'center' },
  deshabilitado: { opacity: 0.5 },
  textoBoton: { color: '#fff', fontWeight: '600' },
})
