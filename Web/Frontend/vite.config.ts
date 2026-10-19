import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Proxy de desenvolvimento para a Web API (.NET). Em produção, configure
// VITE_API_URL ou publique o frontend atrás do mesmo domínio da API.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5080',
        changeOrigin: true
      }
    }
  }
})
