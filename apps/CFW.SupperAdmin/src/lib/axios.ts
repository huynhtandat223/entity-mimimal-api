import axios, {
  AxiosError,
  AxiosInstance,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from 'axios'

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
  (config: InternalAxiosRequestConfig) => {
    // You can add auth token here if needed
    // const token = localStorage.getItem('token');
    // if (token) {
    //   config.headers.Authorization = `Bearer ${token}`;
    // }
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
      // Handle response error
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
