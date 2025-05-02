import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import { postRefresh } from '@/api/cfw-apphost/cfw-apphost'

// Storage keys
const AUTH_STORAGE_KEY = 'auth-storage'
const TOKEN_EXPIRY_TIME = 24 * 60 * 60 * 1000 // 1 day in milliseconds

interface AuthToken {
  accessToken: string
  refreshToken: string
  expiresIn: number
  expiryTime: number // Timestamp when the token expires
}

interface AuthUser {
  accountNo: string
  email: string
  role: string[]
  exp: number
}

interface AuthState {
  auth: {
    user: AuthUser | null
    setUser: (user: AuthUser | null) => void
    token: AuthToken | null
    setToken: (tokenData: Omit<AuthToken, 'expiryTime'>) => void
    getAccessToken: () => Promise<string>
    resetTokens: () => void
    reset: () => void
    isAuthenticated: boolean
  }
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      auth: {
        user: null,
        token: null,
        isAuthenticated: false,

        setUser: (user) =>
          set((state) => ({
            ...state,
            auth: { ...state.auth, user, isAuthenticated: !!user },
          })),

        setToken: (tokenData) => {
          const now = Date.now()
          const expiryTime = now + tokenData.expiresIn * 1000
          const token: AuthToken = {
            ...tokenData,
            expiryTime,
          }

          set((state) => ({
            ...state,
            auth: {
              ...state.auth,
              token,
              isAuthenticated: true,
            },
          }))
        },

        getAccessToken: async () => {
          const { auth } = get()
          const now = Date.now()

          // If no token exists, return empty string
          if (!auth.token) return ''

          // If token is still valid, return it
          if (auth.token.expiryTime > now) {
            return auth.token.accessToken
          }

          // If refresh token is available and not expired (1 day)
          if (
            auth.token.refreshToken &&
            auth.token.expiryTime > now - TOKEN_EXPIRY_TIME
          ) {
            try {
              // Try to refresh the token
              const response = await postRefresh({
                refreshToken: auth.token.refreshToken,
              })

              // Update token in store
              auth.setToken({
                accessToken: response.accessToken,
                refreshToken: response.refreshToken,
                expiresIn: response.expiresIn,
              })

              return response.accessToken
            } catch (_error) {
              // If refresh fails, reset tokens and return empty
              auth.resetTokens()
              return ''
            }
          } else {
            // Token expired and can't be refreshed, reset
            auth.resetTokens()
            return ''
          }
        },

        resetTokens: () =>
          set((state) => ({
            ...state,
            auth: {
              ...state.auth,
              token: null,
              isAuthenticated: false,
            },
          })),

        reset: () =>
          set((state) => ({
            ...state,
            auth: {
              ...state.auth,
              user: null,
              token: null,
              isAuthenticated: false,
            },
          })),
      },
    }),
    {
      name: AUTH_STORAGE_KEY,
      partialize: (state) => ({
        auth: {
          token: state.auth.token,
          user: state.auth.user,
        },
      }),
    }
  )
)

// Helper hook to get only authenticated status
export const useIsAuthenticated = () =>
  useAuthStore((state) => state.auth.isAuthenticated)
