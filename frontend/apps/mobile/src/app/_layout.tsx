import { Stack } from 'expo-router'
import * as SplashScreen from 'expo-splash-screen'
import { useEffect } from 'react'
import { useSesion } from '@/lib/sesion'

SplashScreen.preventAutoHideAsync()

export default function RootLayout() {
  const hidratada = useSesion((s) => s.hidratada)
  const conSesion = useSesion((s) => s.token !== null)

  useEffect(() => {
    if (hidratada) SplashScreen.hideAsync()
  }, [hidratada])

  if (!hidratada) return null

  return (
    <Stack screenOptions={{ headerShown: false }}>
      <Stack.Protected guard={conSesion}>
        <Stack.Screen name="index" />
      </Stack.Protected>
      <Stack.Protected guard={!conSesion}>
        <Stack.Screen name="login" />
      </Stack.Protected>
    </Stack>
  )
}
