import axios, {
  AxiosError,
  AxiosInstance,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from 'axios'
import { useAuthStore } from '@/stores/authStore'

// Get environment variables
const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000'

// Log environment in development
if (import.meta.env.DEV) {
  console.log('API URL:', apiUrl)
  console.log('Environment:', import.meta.env.MODE)
}

// Create axios instance with default config
const axiosInstance: AxiosInstance = axios.create({
  baseURL: apiUrl,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
})

// Request interceptor
axiosInstance.interceptors.request.use(
  async (config: InternalAxiosRequestConfig) => {
    // Get token from auth store
    const accessToken = await useAuthStore.getState().auth.getAccessToken()

    // Add token to request if it exists
    if (accessToken) {
      config.headers.Authorization = `Bearer ${accessToken}`
    }

    return config
  },
  (error: AxiosError) => {
    return Promise.reject(error)
  }
)

// Response interceptor
axiosInstance.interceptors.response.use(
  (response: AxiosResponse) => {
    return response
  },
  (error: AxiosError) => {
    // Handle errors here
    if (error.response) {
      // The request was made and the server responded with a status code
      // that falls out of the range of 2xx

      // Handle 401 errors globally - logout user
      if (error.response.status === 401) {
        // Don't logout on login endpoints
        const isAuthEndpoint =
          error.config?.url?.includes('/login') ||
          error.config?.url?.includes('/refresh')

        if (!isAuthEndpoint) {
          useAuthStore.getState().auth.resetTokens()
        }
      }
    } else if (error.request) {
      // The request was made but no response was received
      // Handle request error
    } else {
      // Something happened in setting up the request that triggered an Error
      // Handle other errors
    }
    return Promise.reject(error)
  }
)

// Export the custom axios instance
export const customAxios = axiosInstance

// Orval mutator function
export const customAxiosFunction = async <T>(
  config: InternalAxiosRequestConfig
): Promise<T> => {
  const { data } = await axiosInstance.request<T>(config)
  return data
}
