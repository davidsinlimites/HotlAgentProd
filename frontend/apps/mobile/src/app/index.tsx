import { useEffect, useRef, useState } from 'react'
import { FlatList, KeyboardAvoidingView, Platform, Pressable, StyleSheet, Text, TextInput, View } from 'react-native'
import { SafeAreaView } from 'react-native-safe-area-context'
import { almacenChat, useChat } from '@/lib/api'
import { useSesion } from '@/lib/sesion'

export default function Chat() {
  const mensajes = useChat((s) => s.mensajes)
  const enCurso = useChat((s) => s.enCurso)
  const cerrar = useSesion((s) => s.cerrar)
  const [texto, setTexto] = useState('')
  const lista = useRef<FlatList>(null)

  useEffect(() => {
    lista.current?.scrollToEnd({ animated: true })
  }, [mensajes])

  function enviar() {
    const t = texto
    setTexto('')
    void almacenChat.getState().enviar(t)
  }

  function salir() {
    almacenChat.getState().limpiar()
    cerrar()
  }

  return (
    <SafeAreaView style={estilos.pantalla}>
      <View style={estilos.cabecera}>
        <Text style={estilos.titulo}>Asistente del hotel</Text>
        <Pressable onPress={salir} accessibilityRole="button">
          <Text style={estilos.salir}>Salir</Text>
        </Pressable>
      </View>

      <KeyboardAvoidingView behavior={Platform.OS === 'ios' ? 'padding' : undefined} style={estilos.cuerpo}>
        <FlatList
          ref={lista}
          data={mensajes}
          keyExtractor={(m) => m.id}
          contentContainerStyle={estilos.lista}
          ListEmptyComponent={
            <Text style={estilos.vacio}>Pregúntame por los restaurantes, el menú, las actividades o tu estancia.</Text>
          }
          renderItem={({ item: m }) => (
            <View style={[estilos.fila, m.rol === 'usuario' ? estilos.derecha : estilos.izquierda]}>
              <Text
                style={[
                  estilos.burbuja,
                  m.rol === 'usuario' ? estilos.usuario : m.error ? estilos.fallo : estilos.asistente,
                ]}
              >
                {m.texto || (enCurso ? '…' : '')}
              </Text>
            </View>
          )}
        />

        <View style={estilos.barra}>
          <TextInput
            value={texto}
            onChangeText={setTexto}
            onSubmitEditing={enviar}
            maxLength={2000}
            placeholder="Escribe tu mensaje"
            style={estilos.campo}
            returnKeyType="send"
          />
          {enCurso ? (
            <Pressable onPress={() => almacenChat.getState().cancelar()} style={estilos.secundario} accessibilityRole="button">
              <Text>Detener</Text>
            </Pressable>
          ) : (
            <Pressable
              onPress={enviar}
              disabled={!texto.trim()}
              style={[estilos.boton, !texto.trim() && estilos.deshabilitado]}
              accessibilityRole="button"
            >
              <Text style={estilos.textoBoton}>Enviar</Text>
            </Pressable>
          )}
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  )
}

const estilos = StyleSheet.create({
  pantalla: { flex: 1, backgroundColor: '#fff' },
  cuerpo: { flex: 1 },
  cabecera: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', padding: 16, borderBottomWidth: 1, borderColor: '#e2e8f0' },
  titulo: { fontSize: 16, fontWeight: '600', color: '#0f172a' },
  salir: { color: '#475569', textDecorationLine: 'underline' },
  lista: { padding: 16, gap: 12, flexGrow: 1 },
  vacio: { color: '#64748b' },
  fila: { flexDirection: 'row' },
  derecha: { justifyContent: 'flex-end' },
  izquierda: { justifyContent: 'flex-start' },
  burbuja: { maxWidth: '85%', borderRadius: 16, paddingHorizontal: 14, paddingVertical: 8, overflow: 'hidden' },
  usuario: { backgroundColor: '#0f172a', color: '#fff' },
  asistente: { backgroundColor: '#f1f5f9', color: '#0f172a' },
  fallo: { backgroundColor: '#fef2f2', color: '#991b1b' },
  barra: { flexDirection: 'row', gap: 8, padding: 12, borderTopWidth: 1, borderColor: '#e2e8f0' },
  campo: { flex: 1, borderWidth: 1, borderColor: '#cbd5e1', borderRadius: 8, paddingHorizontal: 12, paddingVertical: 8, fontSize: 16 },
  boton: { backgroundColor: '#0f172a', borderRadius: 8, paddingHorizontal: 16, justifyContent: 'center' },
  deshabilitado: { opacity: 0.5 },
  textoBoton: { color: '#fff', fontWeight: '600' },
  secundario: { borderWidth: 1, borderColor: '#cbd5e1', borderRadius: 8, paddingHorizontal: 16, justifyContent: 'center' },
})
