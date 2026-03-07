import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

const port = parseInt(process.env.PORT || '5173')

const gatewayUrl =
  process.env.GATEWAY_URL ||
  process.env['services__chatbot-gateway__https__0'] ||
  process.env['services__chatbot-gateway__http__0'] ||
  'http://localhost:5100'

const keycloakUrl =
  process.env.KEYCLOAK_URL ||
  process.env['services__keycloak__http__0'] ||
  'http://localhost:8080'

console.log(`[vite] Gateway proxy target: ${gatewayUrl}`)
console.log(`[vite] Keycloak URL: ${keycloakUrl}`)

export default defineConfig({
  plugins: [react(), tailwindcss()],
  define: {
    'import.meta.env.VITE_KEYCLOAK_URL': JSON.stringify(keycloakUrl),
  },
  server: {
    port,
    host: true,
    allowedHosts: true,
    proxy: {
      '/api': {
        target: gatewayUrl,
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
