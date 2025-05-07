import axios, {
  AxiosError,
  AxiosInstance,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from 'axios'

// Get environment variables
const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5000'

// Create axios instance with default config
const axiosInstance: AxiosInstance = axios.create({
  baseURL: apiUrl,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
})

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
      console.error('API Error:', error.response.status, error.response.data)
    }
    return Promise.reject(error)
  }
)

// Export the custom axios function
export const customAxiosFunction = async <T>(
  config: InternalAxiosRequestConfig & { headers?: Record<string, string> }
): Promise<T> => {
  const response = await axiosInstance.request<T>({
    ...config,
    headers: {
      'Content-Type': 'application/json',
      ...config.headers,
    },
  })
  return response.data
}
