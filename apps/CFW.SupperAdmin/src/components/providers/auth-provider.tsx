import { useEffect, useRef } from 'react'
import { AxiosError } from 'axios'
import { useGetManageInfo } from '@/api/cfw-apphost/cfw-apphost'
import { useIsAuthenticated } from '@/stores/authStore'

export function AuthProvider({ children }: { children: React.ReactNode }) {
  // Use a ref to track if we've already redirected
  const hasRedirected = useRef(false)
  const isAuthenticated = useIsAuthenticated()

  // Skip the auth check if we're already on the sign-in page
  const isSignInPage = window.location.pathname === '/sign-in'

  const { isError, error } = useGetManageInfo({
    query: {
      retry: false,
      // Disable the query on the sign-in page or when not authenticated
      enabled: !isSignInPage && isAuthenticated,
    },
  })

  useEffect(() => {
    // If we're already on the sign-in page or have already redirected, don't do anything
    if (isSignInPage || hasRedirected.current || !isAuthenticated) {
      return
    }

    if (isError) {
      // Check for specific error types
      if (error instanceof AxiosError) {
        // Handle both 401 status and network errors (which could be CORS)
        if (
          error.response?.status === 401 ||
          error.code === 'ERR_NETWORK' ||
          error.message.includes('Network Error')
        ) {
          // Set the ref to true to prevent future redirects
          hasRedirected.current = true
          // Redirect to sign-in
          window.location.href = '/sign-in'
        }
      }
    }
  }, [isError, error, isSignInPage, isAuthenticated])

  return <>{children}</>
}
