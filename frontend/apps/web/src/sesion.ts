import { create } from 'zustand'
import { persist, createJSONStorage } from 'zustand/middleware'

interface Sesion {
  token: string | null
  caduca: string | null
  guardar: (token: string, caduca: string) => void
  cerrar: () => void
}

// sessionStorage: la sesion muere al cerrar la pestana (un huesped puede usar un equipo compartido)
export const useSesion = create<Sesion>()(
  persist(
    (set) => ({
      token: null,
      caduca: null,
      guardar: (token, caduca) => set({ token, caduca }),
      cerrar: () => set({ token: null, caduca: null }),
    }),
    {
      name: 'hotelagent-sesion',
      storage: createJSONStorage(() => sessionStorage),
      onRehydrateStorage: () => (estado) => {
        if (estado?.caduca && new Date(estado.caduca) <= new Date()) estado.cerrar()
      },
    },
  ),
)
