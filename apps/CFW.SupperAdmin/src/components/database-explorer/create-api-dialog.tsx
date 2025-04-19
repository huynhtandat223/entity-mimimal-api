import { useEffect, useState } from 'react'
import { ApiConfig, AuthorizationMetadata, TableInfo } from '@/types/database'
import { Check, ChevronsUpDown } from 'lucide-react'
import pluralize from 'pluralize'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
} from '@/components/ui/command'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Switch } from '@/components/ui/switch'

interface CreateApiDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  table: TableInfo
  relatedTables: TableInfo[]
  authMetadata: AuthorizationMetadata
  onSubmit: (config: ApiConfig) => void
}

export function CreateApiDialog({
  open,
  onOpenChange,
  table,
  relatedTables,
  authMetadata,
  onSubmit,
}: CreateApiDialogProps) {
  const [routeName, setRouteName] = useState(
    `/api/${pluralize(table.name.toLowerCase())}`
  )
  const [selectedColumns, setSelectedColumns] = useState<string[]>([])
  const [selectedRelatedTables, setSelectedRelatedTables] = useState<
    Record<string, string[]>
  >({})
  const [requiresAuth, setRequiresAuth] = useState(true)
  const [selectedRoles, setSelectedRoles] = useState<string[]>([])
  const [selectedPermissions, setSelectedPermissions] = useState<string[]>([])
  const [selectedPolicies, setSelectedPolicies] = useState<string[]>([])

  // Auto-select all columns and related columns on mount
  useEffect(() => {
    if (open) {
      // Select all columns from the main table
      setSelectedColumns(table.columns.map((col) => col.name))

      // Select all columns from related tables
      const relatedColumns: Record<string, string[]> = {}
      relatedTables.forEach((relatedTable) => {
        relatedColumns[relatedTable.name] = relatedTable.columns.map(
          (col) => col.name
        )
      })
      setSelectedRelatedTables(relatedColumns)
    }
  }, [open, table, relatedTables])

  const handleSubmit = () => {
    const config: ApiConfig = {
      routeName,
      selectedColumns,
      relatedTables: Object.entries(selectedRelatedTables).map(
        ([tableName, columns]) => ({
          tableName,
          columns,
        })
      ),
      authentication: requiresAuth,
      authorization: {
        roles: selectedRoles,
        permissions: selectedPermissions,
        policies: selectedPolicies,
      },
      columnBehaviors: [],
    }
    onSubmit(config)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='flex max-h-[90vh] max-w-2xl flex-col overflow-hidden'>
        <DialogHeader>
          <DialogTitle>Create API Endpoint</DialogTitle>
          <DialogDescription>
            Configure the API endpoint for {table.name} table
          </DialogDescription>
        </DialogHeader>

        <div className='flex-1 overflow-auto py-4'>
          <div className='space-y-6'>
            {/* Route Configuration */}
            <div className='space-y-2'>
              <Label>Route Path</Label>
              <Input
                value={routeName}
                onChange={(e) => setRouteName(e.target.value)}
                placeholder='/api/route-name'
              />
            </div>

            {/* Column Selection */}
            <div className='space-y-2'>
              <Label>Select Columns</Label>
              <ScrollArea className='h-[200px] rounded-md border p-4'>
                {table.columns.map((column) => (
                  <div
                    key={column.name}
                    className='flex items-center space-x-2 py-2'
                  >
                    <Checkbox
                      checked={selectedColumns.includes(column.name)}
                      onCheckedChange={(checked) => {
                        setSelectedColumns(
                          checked
                            ? [...selectedColumns, column.name]
                            : selectedColumns.filter((c) => c !== column.name)
                        )
                      }}
                    />
                    <span>{column.name}</span>
                  </div>
                ))}
              </ScrollArea>
            </div>

            {/* Related Tables */}
            {relatedTables.length > 0 && (
              <div className='space-y-2'>
                <Label>Include Related Tables</Label>
                <ScrollArea className='h-[200px] rounded-md border p-4'>
                  {relatedTables.map((relatedTable) => (
                    <div key={relatedTable.name} className='space-y-2 py-2'>
                      <div className='flex items-center space-x-2'>
                        <h4 className='font-medium'>{relatedTable.name}</h4>
                      </div>
                      <div className='space-y-1 pl-6'>
                        {relatedTable.columns.map((column) => (
                          <div
                            key={column.name}
                            className='flex items-center space-x-2'
                          >
                            <Checkbox
                              checked={selectedRelatedTables[
                                relatedTable.name
                              ]?.includes(column.name)}
                              onCheckedChange={(checked) => {
                                const currentColumns =
                                  selectedRelatedTables[relatedTable.name] || []
                                setSelectedRelatedTables({
                                  ...selectedRelatedTables,
                                  [relatedTable.name]: checked
                                    ? [...currentColumns, column.name]
                                    : currentColumns.filter(
                                        (c) => c !== column.name
                                      ),
                                })
                              }}
                            />
                            <span>{column.name}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  ))}
                </ScrollArea>
              </div>
            )}

            {/* Authentication & Authorization */}
            <div className='space-y-4'>
              <div className='flex items-center space-x-2'>
                <Switch
                  checked={requiresAuth}
                  onCheckedChange={setRequiresAuth}
                />
                <Label>Require Authentication</Label>
              </div>

              {requiresAuth && (
                <div className='space-y-4 pl-6'>
                  {/* Roles */}
                  <div className='space-y-2'>
                    <Label>Roles</Label>
                    <Popover>
                      <PopoverTrigger asChild>
                        <Button
                          variant='outline'
                          className='w-full justify-between'
                        >
                          {selectedRoles.length > 0
                            ? `${selectedRoles.length} selected`
                            : 'Select roles'}
                          <ChevronsUpDown className='ml-2 h-4 w-4 shrink-0 opacity-50' />
                        </Button>
                      </PopoverTrigger>
                      <PopoverContent className='w-full p-0'>
                        <Command>
                          <CommandInput placeholder='Search roles...' />
                          <CommandEmpty>No roles found.</CommandEmpty>
                          <CommandGroup>
                            {authMetadata.roles.map((role) => (
                              <CommandItem
                                key={role}
                                onSelect={() => {
                                  setSelectedRoles(
                                    selectedRoles.includes(role)
                                      ? selectedRoles.filter((r) => r !== role)
                                      : [...selectedRoles, role]
                                  )
                                }}
                              >
                                <Check
                                  className={cn(
                                    'mr-2 h-4 w-4',
                                    selectedRoles.includes(role)
                                      ? 'opacity-100'
                                      : 'opacity-0'
                                  )}
                                />
                                {role}
                              </CommandItem>
                            ))}
                          </CommandGroup>
                        </Command>
                      </PopoverContent>
                    </Popover>
                  </div>

                  {/* Permissions */}
                  <div className='space-y-2'>
                    <Label>Permissions</Label>
                    <ScrollArea className='h-[100px] rounded-md border p-2'>
                      {authMetadata.permissions.map((permission) => (
                        <div
                          key={permission}
                          className='flex items-center space-x-2 py-1'
                        >
                          <Checkbox
                            checked={selectedPermissions.includes(permission)}
                            onCheckedChange={(checked) => {
                              setSelectedPermissions(
                                checked
                                  ? [...selectedPermissions, permission]
                                  : selectedPermissions.filter(
                                      (p) => p !== permission
                                    )
                              )
                            }}
                          />
                          <span>{permission}</span>
                        </div>
                      ))}
                    </ScrollArea>
                  </div>

                  {/* Policies */}
                  <div className='space-y-2'>
                    <Label>Policies</Label>
                    <ScrollArea className='h-[100px] rounded-md border p-2'>
                      {authMetadata.policies.map((policy) => (
                        <div
                          key={policy}
                          className='flex items-center space-x-2 py-1'
                        >
                          <Checkbox
                            checked={selectedPolicies.includes(policy)}
                            onCheckedChange={(checked) => {
                              setSelectedPolicies(
                                checked
                                  ? [...selectedPolicies, policy]
                                  : selectedPolicies.filter((p) => p !== policy)
                              )
                            }}
                          />
                          <span>{policy}</span>
                        </div>
                      ))}
                    </ScrollArea>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button variant='outline' onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={handleSubmit}>Create API</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
