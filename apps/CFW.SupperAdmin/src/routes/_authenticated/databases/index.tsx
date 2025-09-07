import { useState } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { Edit3, Plus, TestTube, Trash2 } from 'lucide-react'
import { postApiV1DatabasesTablesDatabasesTables } from '@/api/databases-tables/databases-tables'
import { DatabaseTableViewModel } from '@/api/model'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { TypeSafeODataTable } from '@/components/ui/odata-table'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { PageLayout } from '@/components/layout/PageLayout'

export const Route = createFileRoute('/_authenticated/databases/')({
  component: DatabasesPage,
})

// Database provider types
enum DatabaseProvider {
  MSSQL = 0,
  PostgreSQL = 1,
  MySQL = 2,
  Oracle = 3,
}

// Configuration model for each provider
interface DatabaseConfig {
  id?: string
  name: string
  provider: DatabaseProvider
  config: {
    MSSQL?: {
      server: string
      port?: number
      database: string
      username?: string
      password?: string
      integratedSecurity?: boolean
      encrypt?: boolean
      trustServerCertificate?: boolean
      multipleActiveResultSets?: boolean
    }
    PostgreSQL?: {
      host: string
      port?: number
      database: string
      username?: string
      password?: string
      sslMode?: 'disable' | 'require' | 'verify-ca' | 'verify-full'
      schema?: string
    }
    MySQL?: {
      host: string
      port?: number
      database: string
      username?: string
      password?: string
      charset?: string
      sslMode?: boolean
    }
    Oracle?: {
      host: string
      port?: number
      serviceName: string
      username?: string
      password?: string
      connectionType?: 'basic' | 'tns'
    }
  }
}

const DatabaseProviders = {
  [DatabaseProvider.MSSQL]: {
    name: 'Microsoft SQL Server',
    icon: '🗄️',
    defaultPort: 1433,
  },
  [DatabaseProvider.PostgreSQL]: {
    name: 'PostgreSQL',
    icon: '🐘',
    defaultPort: 5432,
  },
  [DatabaseProvider.MySQL]: { name: 'MySQL', icon: '🐬', defaultPort: 3306 },
  [DatabaseProvider.Oracle]: { name: 'Oracle', icon: '🔴', defaultPort: 1521 },
}

const DatabaseConfigForm = ({
  database,
  onSave,
  onCancel,
}: {
  database?: DatabaseConfig
  onSave: (config: DatabaseConfig) => void
  onCancel: () => void
}) => {
  const [formData, setFormData] = useState<DatabaseConfig>(
    database || {
      name: '',
      provider: DatabaseProvider.MSSQL,
      config: {
        MSSQL: {
          server: '',
          port: 1433,
          database: '',
          username: '',
          password: '',
          integratedSecurity: false,
          encrypt: true,
          trustServerCertificate: true,
          multipleActiveResultSets: true,
        },
      },
    }
  )

  const [isTestingConnection, setIsTestingConnection] = useState(false)
  const [testResult, setTestResult] = useState<{
    success: boolean
    message: string
  } | null>(null)

  const handleProviderChange = (value: string) => {
    const provider = parseInt(value) as DatabaseProvider
    const providerInfo = DatabaseProviders[provider]

    setFormData((prev) => ({
      ...prev,
      provider,
      config: {
        [DatabaseProvider[provider]]: getDefaultConfigForProvider(
          provider,
          providerInfo.defaultPort
        ),
      },
    }))
  }

  const getDefaultConfigForProvider = (
    provider: DatabaseProvider,
    defaultPort: number
  ) => {
    switch (provider) {
      case DatabaseProvider.MSSQL:
        return {
          server: '',
          port: defaultPort,
          database: '',
          username: '',
          password: '',
          integratedSecurity: false,
          encrypt: true,
          trustServerCertificate: true,
          multipleActiveResultSets: true,
        }
      case DatabaseProvider.PostgreSQL:
        return {
          host: '',
          port: defaultPort,
          database: '',
          username: '',
          password: '',
          sslMode: 'require' as const,
          schema: 'public',
        }
      case DatabaseProvider.MySQL:
        return {
          host: '',
          port: defaultPort,
          database: '',
          username: '',
          password: '',
          charset: 'utf8mb4',
          sslMode: true,
        }
      case DatabaseProvider.Oracle:
        return {
          host: '',
          port: defaultPort,
          serviceName: '',
          username: '',
          password: '',
          connectionType: 'basic' as const,
        }
      default:
        return {}
    }
  }

  const updateProviderConfig = (field: string, value: any) => {
    const providerKey = DatabaseProvider[
      formData.provider
    ] as keyof typeof formData.config

    setFormData((prev) => ({
      ...prev,
      config: {
        ...prev.config,
        [providerKey]: {
          ...prev.config[providerKey],
          [field]: value,
        },
      },
    }))
  }

  const testConnection = async () => {
    setIsTestingConnection(true)
    setTestResult(null)

    try {
      // Call your API endpoint to test the connection
      // const result = await postApiV1DatabasesTestConnection(formData)

      // Mock the test for now
      await new Promise((resolve) => setTimeout(resolve, 2000))

      setTestResult({
        success: true,
        message: 'Connection successful!',
      })
    } catch (error) {
      setTestResult({
        success: false,
        message: 'Connection failed. Please check your settings.',
      })
    } finally {
      setIsTestingConnection(false)
    }
  }

  const renderProviderSpecificFields = () => {
    const provider = formData.provider
    const config = formData.config[
      DatabaseProvider[provider] as keyof typeof formData.config
    ] as any

    if (!config) return null

    switch (provider) {
      case DatabaseProvider.MSSQL:
        return (
          <>
            <div className='grid grid-cols-3 gap-4'>
              <div className='col-span-2 space-y-2'>
                <Label>Server</Label>
                <Input
                  value={config.server || ''}
                  onChange={(e) =>
                    updateProviderConfig('server', e.target.value)
                  }
                  placeholder='localhost\\SQLEXPRESS'
                />
              </div>
              <div className='space-y-2'>
                <Label>Port</Label>
                <Input
                  type='number'
                  value={config.port || ''}
                  onChange={(e) =>
                    updateProviderConfig(
                      'port',
                      parseInt(e.target.value) || null
                    )
                  }
                  placeholder='1433'
                />
              </div>
            </div>
            <div className='space-y-2'>
              <Label>Database</Label>
              <Input
                value={config.database || ''}
                onChange={(e) =>
                  updateProviderConfig('database', e.target.value)
                }
                placeholder='MyDatabase'
              />
            </div>
            <div className='grid grid-cols-2 gap-4'>
              <div className='space-y-2'>
                <Label>Username</Label>
                <Input
                  value={config.username || ''}
                  onChange={(e) =>
                    updateProviderConfig('username', e.target.value)
                  }
                  placeholder='sa'
                  disabled={config.integratedSecurity}
                />
              </div>
              <div className='space-y-2'>
                <Label>Password</Label>
                <Input
                  type='password'
                  value={config.password || ''}
                  onChange={(e) =>
                    updateProviderConfig('password', e.target.value)
                  }
                  disabled={config.integratedSecurity}
                />
              </div>
            </div>
            <div className='space-y-2'>
              <label className='flex items-center space-x-2'>
                <input
                  type='checkbox'
                  checked={config.integratedSecurity || false}
                  onChange={(e) =>
                    updateProviderConfig('integratedSecurity', e.target.checked)
                  }
                />
                <span>Use Integrated Security (Windows Authentication)</span>
              </label>
            </div>
          </>
        )

      case DatabaseProvider.PostgreSQL:
        return (
          <>
            <div className='grid grid-cols-3 gap-4'>
              <div className='col-span-2 space-y-2'>
                <Label>Host</Label>
                <Input
                  value={config.host || ''}
                  onChange={(e) => updateProviderConfig('host', e.target.value)}
                  placeholder='localhost'
                />
              </div>
              <div className='space-y-2'>
                <Label>Port</Label>
                <Input
                  type='number'
                  value={config.port || ''}
                  onChange={(e) =>
                    updateProviderConfig(
                      'port',
                      parseInt(e.target.value) || null
                    )
                  }
                  placeholder='5432'
                />
              </div>
            </div>
            <div className='space-y-2'>
              <Label>Database</Label>
              <Input
                value={config.database || ''}
                onChange={(e) =>
                  updateProviderConfig('database', e.target.value)
                }
                placeholder='myapp'
              />
            </div>
            <div className='grid grid-cols-2 gap-4'>
              <div className='space-y-2'>
                <Label>Username</Label>
                <Input
                  value={config.username || ''}
                  onChange={(e) =>
                    updateProviderConfig('username', e.target.value)
                  }
                  placeholder='postgres'
                />
              </div>
              <div className='space-y-2'>
                <Label>Password</Label>
                <Input
                  type='password'
                  value={config.password || ''}
                  onChange={(e) =>
                    updateProviderConfig('password', e.target.value)
                  }
                />
              </div>
            </div>
            <div className='grid grid-cols-2 gap-4'>
              <div className='space-y-2'>
                <Label>Schema</Label>
                <Input
                  value={config.schema || ''}
                  onChange={(e) =>
                    updateProviderConfig('schema', e.target.value)
                  }
                  placeholder='public'
                />
              </div>
              <div className='space-y-2'>
                <Label>SSL Mode</Label>
                <Select
                  value={config.sslMode}
                  onValueChange={(value) =>
                    updateProviderConfig('sslMode', value)
                  }
                >
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value='disable'>Disable</SelectItem>
                    <SelectItem value='require'>Require</SelectItem>
                    <SelectItem value='verify-ca'>Verify CA</SelectItem>
                    <SelectItem value='verify-full'>Verify Full</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>
          </>
        )

      // Add similar cases for MySQL and Oracle...
      default:
        return <div>Provider configuration not implemented</div>
    }
  }

  return (
    <div className='space-y-6'>
      <div className='space-y-4'>
        <div className='space-y-2'>
          <Label>Connection Name</Label>
          <Input
            value={formData.name}
            onChange={(e) =>
              setFormData((prev) => ({ ...prev, name: e.target.value }))
            }
            placeholder='My Database Connection'
          />
        </div>

        <div className='space-y-2'>
          <Label>Database Provider</Label>
          <Select
            value={formData.provider.toString()}
            onValueChange={handleProviderChange}
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {Object.entries(DatabaseProviders).map(([key, provider]) => (
                <SelectItem key={key} value={key}>
                  <div className='flex items-center gap-2'>
                    <span>{provider.icon}</span>
                    <span>{provider.name}</span>
                  </div>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {renderProviderSpecificFields()}

        <div className='flex gap-2'>
          <Button
            variant='outline'
            onClick={testConnection}
            disabled={isTestingConnection}
          >
            <TestTube className='mr-2 h-4 w-4' />
            {isTestingConnection ? 'Testing...' : 'Test Connection'}
          </Button>
        </div>

        {testResult && (
          <div
            className={`rounded-md p-3 ${testResult.success ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'}`}
          >
            {testResult.message}
          </div>
        )}
      </div>

      <div className='flex justify-end gap-2 border-t pt-4'>
        <Button variant='outline' onClick={onCancel}>
          Cancel
        </Button>
        <Button onClick={() => onSave(formData)} disabled={!formData.name}>
          Save Database
        </Button>
      </div>
    </div>
  )
}

export default function DatabasesPage() {
  const [selectedConfigurations, setSelectedConfigurations] = useState<
    DatabaseConfig[]
  >([
    {
      id: '1',
      name: 'Production SQL Server',
      provider: DatabaseProvider.MSSQL,
      config: {
        MSSQL: {
          server: 'localhost\\SQLEXPRESS',
          database: 'cirrusvm',
          username: 'sa',
          password: '123456',
          encrypt: true,
          trustServerCertificate: true,
          multipleActiveResultSets: true,
        },
      },
    },
  ])

  const [selectedDatabase, setSelectedDatabase] =
    useState<DatabaseConfig | null>(null)
  const [isDialogOpen, setIsDialogOpen] = useState(false)
  const [selectedTables, setSelectedTables] = useState<
    DatabaseTableViewModel[]
  >([])

  const handleAddDatabase = () => {
    setSelectedDatabase(null)
    setIsDialogOpen(true)
  }

  const handleEditDatabase = (database: DatabaseConfig) => {
    setSelectedDatabase(database)
    setIsDialogOpen(true)
  }

  const handleSaveDatabase = (config: DatabaseConfig) => {
    if (selectedDatabase) {
      setSelectedConfigurations((prev) =>
        prev.map((db) =>
          db.id === selectedDatabase.id
            ? { ...config, id: selectedDatabase.id }
            : db
        )
      )
    } else {
      setSelectedConfigurations((prev) => [
        ...prev,
        { ...config, id: Date.now().toString() },
      ])
    }
    setIsDialogOpen(false)
    setSelectedDatabase(null)
  }

  const handleDeleteDatabase = (databaseId: string) => {
    setSelectedConfigurations((prev) =>
      prev.filter((db) => db.id !== databaseId)
    )
  }

  const loadTablesForDatabase = async (database: DatabaseConfig) => {
    try {
      // Send the configuration object to the backend to establish connection and get tables
      const result = await postApiV1DatabasesTablesDatabasesTables({
        databaseProvider: database.provider,
        databaseConfiguration: database.config, // Send the config object instead of connection string
      })
      return result
    } catch (error) {
      console.error('Failed to load tables:', error)
      throw error
    }
  }

  return (
    <PageLayout
      title='Databases'
      description='Manage your database connections and view tables'
    >
      <div className='space-y-6'>
        {/* Database Configurations Section */}
        <Card>
          <CardHeader className='flex flex-row items-center justify-between'>
            <div>
              <CardTitle>Database Connections</CardTitle>
              <p className='text-sm text-gray-600'>
                Configure your database connections
              </p>
            </div>
            <Button onClick={handleAddDatabase}>
              <Plus className='mr-2 h-4 w-4' />
              Add Database
            </Button>
          </CardHeader>
          <CardContent>
            <div className='space-y-3'>
              {selectedConfigurations.map((db) => {
                const provider = DatabaseProviders[db.provider]
                return (
                  <div
                    key={db.id}
                    className='flex items-center justify-between rounded-lg border p-3'
                  >
                    <div className='flex items-center gap-3'>
                      <span className='text-lg'>{provider.icon}</span>
                      <div>
                        <h4 className='font-medium'>{db.name}</h4>
                        <p className='text-sm text-gray-600'>{provider.name}</p>
                      </div>
                    </div>
                    <div className='flex items-center gap-2'>
                      <Badge variant='secondary'>Connected</Badge>
                      <Button
                        variant='ghost'
                        size='sm'
                        onClick={() => handleEditDatabase(db)}
                      >
                        <Edit3 className='h-4 w-4' />
                      </Button>
                      <Button
                        variant='ghost'
                        size='sm'
                        onClick={() => handleDeleteDatabase(db.id!)}
                        className='text-red-600 hover:text-red-800'
                      >
                        <Trash2 className='h-4 w-4' />
                      </Button>
                    </div>
                  </div>
                )
              })}
            </div>
          </CardContent>
        </Card>

        {/* Tables Section */}
        {selectedConfigurations.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle>Database Tables</CardTitle>
              <p className='text-sm text-gray-600'>
                Select tables from your configured databases
              </p>
            </CardHeader>
            <CardContent>
              <TypeSafeODataTable<DatabaseTableViewModel>
                columns={[
                  'name',
                  'schema',
                  'rowCount',
                  (_row) => (
                    <div className='flex justify-end gap-2'>
                      <Button
                        variant='ghost'
                        size='sm'
                        onClick={() => {
                          // Handle table selection
                        }}
                      >
                        Select
                      </Button>
                    </div>
                  ),
                ]}
                queryFn={async (params) => {
                  // Use the first configured database for now
                  const primaryDb = selectedConfigurations[0]
                  if (!primaryDb) {
                    return { data: [], total: 0 }
                  }

                  return await loadTablesForDatabase(primaryDb)
                }}
                selectionMode='multiple'
                onSelectionChange={(selection) => {
                  setSelectedTables(selection)
                }}
              />
            </CardContent>
          </Card>
        )}

        {/* Selected Tables Summary */}
        {selectedTables.length > 0 && (
          <Card>
            <CardHeader>
              <CardTitle>Selected Tables ({selectedTables.length})</CardTitle>
            </CardHeader>
            <CardContent>
              <div className='flex flex-wrap gap-2'>
                {selectedTables.map((table, index) => (
                  <Badge key={index} variant='secondary'>
                    {table.schema
                      ? `${table.schema}.${table.name}`
                      : table.name}
                  </Badge>
                ))}
              </div>
            </CardContent>
          </Card>
        )}

        <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
          <DialogContent className='max-h-[90vh] max-w-2xl overflow-y-auto'>
            <DialogHeader>
              <DialogTitle>
                {selectedDatabase
                  ? 'Edit Database Connection'
                  : 'Add Database Connection'}
              </DialogTitle>
              <DialogDescription>
                Configure your database connection settings. Connection details
                are securely stored on the server.
              </DialogDescription>
            </DialogHeader>
            <DatabaseConfigForm
              database={selectedDatabase || undefined}
              onSave={handleSaveDatabase}
              onCancel={() => setIsDialogOpen(false)}
            />
          </DialogContent>
        </Dialog>
      </div>
    </PageLayout>
  )
}
