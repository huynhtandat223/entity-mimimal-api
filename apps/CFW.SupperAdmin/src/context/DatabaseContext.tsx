import { createContext, ReactNode, useContext, useState } from 'react'
import { ConnectionConfig, DatabaseMetadata } from '@/types/database'
import { generateMockMetadata } from '@/lib/database-mock'

interface DatabaseContextType {
  connectionConfig: ConnectionConfig | null
  isConnecting: boolean
  isConnected: boolean
  error: string | null
  databaseMetadata: DatabaseMetadata | null
  selectedTable: string | null
  connect: (config: ConnectionConfig) => Promise<void>
  disconnect: () => void
  selectTable: (tableName: string) => void
}

const DatabaseContext = createContext<DatabaseContextType | undefined>(
  undefined
)

export function DatabaseProvider({ children }: { children: ReactNode }) {
  const [connectionConfig, setConnectionConfig] =
    useState<ConnectionConfig | null>(null)
  const [isConnecting, setIsConnecting] = useState(false)
  const [isConnected, setIsConnected] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [databaseMetadata, setDatabaseMetadata] =
    useState<DatabaseMetadata | null>(null)
  const [selectedTable, setSelectedTable] = useState<string | null>(null)

  const connect = async (config: ConnectionConfig) => {
    setIsConnecting(true)
    setError(null)

    try {
      // In a real application, this would make an actual database connection
      // For this demo, we'll simulate a connection delay and generate mock data
      await new Promise((resolve) => setTimeout(resolve, 1500))

      const metadata = generateMockMetadata(config.type)
      setDatabaseMetadata(metadata)
      setConnectionConfig(config)
      setIsConnected(true)

      // Select the first table by default if available
      if (metadata.tables.length > 0) {
        setSelectedTable(metadata.tables[0].name)
      }
    } catch (err) {
      setError(
        err instanceof Error ? err.message : 'Failed to connect to database'
      )
    } finally {
      setIsConnecting(false)
    }
  }

  const disconnect = () => {
    setConnectionConfig(null)
    setIsConnected(false)
    setDatabaseMetadata(null)
    setSelectedTable(null)
    setError(null)
  }

  const selectTable = (tableName: string) => {
    setSelectedTable(tableName)
  }

  return (
    <DatabaseContext.Provider
      value={{
        connectionConfig,
        isConnecting,
        isConnected,
        error,
        databaseMetadata,
        selectedTable,
        connect,
        disconnect,
        selectTable,
      }}
    >
      {children}
    </DatabaseContext.Provider>
  )
}

export function useDatabase() {
  const context = useContext(DatabaseContext)
  if (context === undefined) {
    throw new Error('useDatabase must be used within a DatabaseProvider')
  }
  return context
}
