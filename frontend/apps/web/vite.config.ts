import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// En desarrollo la web llama a su propio origen y Vite reenvia a la API local (sin CORS).
// En produccion VITE_API_URL apunta a la API publicada (y esa API debe permitir el origen en CORS_ORIGINS).
const apiLocal = process.env.VITE_PROXY_TARGET ?? 'http://localhost:55002'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    proxy: {
      '/chat': apiLocal,
      '/dev': apiLocal,
      '/health': apiLocal,
    },
  },
})
