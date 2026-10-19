import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// Configuração do Vite. Em desenvolvimento, o frontend chama /api e o Vite faz
// proxy para a Web API. O alvo pode ser trocado por VITE_PROXY_TARGET (ex.: 5099
// no modo demonstração InMemory). Em produção, defina VITE_API_URL.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', '')
  const proxyTarget = env.VITE_PROXY_TARGET || 'http://localhost:5080'

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target: proxyTarget,
          changeOrigin: true
        },
        // Fotos de usuário gravadas pela API (wwwroot/uploads).
        '/uploads': {
          target: proxyTarget,
          changeOrigin: true
        }
      }
    }
  }
})
