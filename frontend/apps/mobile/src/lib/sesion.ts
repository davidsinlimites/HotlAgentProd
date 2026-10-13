import * as SecureStore from 'expo-secure-store'
import { create } from 'zustand'
import { createJSONStorage, persist } from 'zustand/middleware'

interface Sesion {
  token: string | null
  caduca: string | null
  hidratada: boolean
  guardar: (token: string, caduca: string) => void
  cerrar: () => void
}

// El token va en el almacen seguro del sistema (Keychain / Keystore), no en AsyncStorage
export const useSesion = create<Sesion>()(
  persist(
    (set) => ({
      token: null,
      caduca: null,
      hidratada: false,
      guardar: (token, caduca) => set({ token, caduca }),
      cerrar: () => set({ token: null, caduca: null }),
    }),
    {
      name: 'hotelagent-sesion',
      storage: createJSONStorage(() => ({
        getItem: (k) => SecureStore.getItemAsync(k),
        setItem: (k, v) => SecureStore.setItemAsync(k, v),
        removeItem: (k) => SecureStore.deleteItemAsync(k),
      })),
      partialize: (s) => ({ token: s.token, caduca: s.caduca }),
      onRehydrateStorage: () => (estado) => {
        if (estado?.caduca && new Date(estado.caduca) <= new Date()) estado.cerrar()
        useSesion.setState({ hidratada: true })
      },
    },
  ),
)
