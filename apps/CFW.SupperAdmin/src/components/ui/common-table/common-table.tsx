import { ReactNode, useCallback, useEffect, useState } from 'react'
import {
  ChevronLeftIcon,
  ChevronRightIcon,
  Cross2Icon,
  DotsHorizontalIcon,
  MixerHorizontalIcon,
} from '@radix-ui/react-icons'
import { useQuery } from '@tanstack/react-query'
import {
  flexRender,
  getCoreRowModel,
  getFacetedRowModel,
  getFacetedUniqueValues,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  useReactTable,
  type ColumnDef,
  type ColumnFiltersState,
  type Row,
  type SortingState,
  type VisibilityState,
} from '@tanstack/react-table'
import { LucideIcon } from 'lucide-react'
import { axiosInstance } from '@/lib/axios'
import { Button } from '@/components/ui/button'
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
} from '@/components/ui/context-menu'
import {
  DropdownMenu,
  DropdownMenuCheckboxItem,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

interface OpenAPISchema {
  components: {
    schemas: {
      [key: string]: {
        type: string
        properties: {
          [key: string]: {
            type: string
            format?: string
            description?: string
            items?: {
              type: string
              $ref?: string
            }
            $ref?: string
          }
        }
        required?: string[]
      }
    }
  }
}

interface SchemaProperty {
  type: string
  format?: string
  description?: string
  required?: boolean
  items?: {
    type: string
    $ref?: string
  }
  $ref?: string
}

export interface TableDataResponse {
  data: any[]
  total: number
}

export interface ActionColumnDef {
  text: string
  Icon: LucideIcon
  onClick: (row: any) => void
}

type SimpleColumnDef = string | ((row: any) => ReactNode)
type ActionColumn = { type: 'action'; actions: ActionColumnDef[] }
type ColumnDefinition = SimpleColumnDef | ActionColumn

export interface CommonTableProps {
  columns?: ColumnDefinition[]
  apiUrl: string
  queryKey?: string[]
  schemaUrl: string
  schemaName: string
}

export function CommonTable({
  columns: customColumns,
  apiUrl,
  queryKey = ['table'],
  schemaUrl,
  schemaName,
}: CommonTableProps) {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [schemaProperties, setSchemaProperties] = useState<{
    [key: string]: SchemaProperty
  }>({})
  const [rowSelection, setRowSelection] = useState({})
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({})
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([])
  const [sorting, setSorting] = useState<SortingState>([])

  useEffect(() => {
    const loadSchema = async () => {
      if (!schemaUrl || !schemaName) return

      try {
        const response = await axiosInstance.get(schemaUrl)
        const schema = response.data as OpenAPISchema
        console.log('Loaded schema:', schema)

        // Find the schema definition
        const schemaDef = schema.components?.schemas?.[schemaName]
        console.log('Schema definition:', schemaDef)

        if (!schemaDef?.properties) {
          console.error(`Schema ${schemaName} not found or has no properties`)
          return
        }

        setSchemaProperties(schemaDef.properties)
      } catch (error) {
        console.error('Error loading schema:', error)
      }
    }

    loadSchema()
  }, [schemaUrl, schemaName])

  const { data, isLoading } = useQuery({
    queryKey: [...queryKey, page, pageSize],
    queryFn: async () => {
      const response = await axiosInstance.post(`${apiUrl}`, {
        page,
        limit: pageSize,
      })
      const responseData = response.data
      return {
        data: responseData.data ?? [],
        total: responseData.total ?? 0,
      }
    },
  })

  const generateColumns = useCallback(() => {
    if (!schemaProperties) return []

    const columns: ColumnDef<any>[] = Object.entries(schemaProperties).map(
      ([key, prop]) => ({
        accessorKey: key,
        id: key,
        header: prop.description || key.charAt(0).toUpperCase() + key.slice(1),
        cell: ({ row }: { row: Row<any> }) => {
          const value = row.getValue(key)
          if (value === null || value === undefined) return '-'
          if (Array.isArray(value)) return value.join(', ')
          if (key === 'lastOpenTime' && typeof value === 'number') {
            return new Date(value).toLocaleString()
          }
          if (typeof value === 'boolean') return value ? 'Yes' : 'No'
          return String(value)
        },
      })
    )

    // Add actions column if custom columns include action type
    const actionColumn = customColumns?.find(
      (col): col is ActionColumn =>
        typeof col === 'object' && 'type' in col && col.type === 'action'
    )

    if (actionColumn) {
      columns.push({
        id: 'actions',
        header: 'Actions',
        cell: ({ row }: { row: Row<any> }) => {
          return (
            <DropdownMenu modal={false}>
              <DropdownMenuTrigger asChild>
                <Button
                  variant='ghost'
                  className='data-[state=open]:bg-muted flex h-8 w-8 p-0'
                >
                  <DotsHorizontalIcon className='h-4 w-4' />
                  <span className='sr-only'>Open menu</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align='end' className='w-[160px]'>
                {actionColumn.actions.map((action, index) => (
                  <DropdownMenuItem
                    key={index}
                    onClick={() => action.onClick(row.original)}
                  >
                    <action.Icon className='mr-2 h-4 w-4' />
                    {action.text}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          )
        },
      })
    }

    return columns
  }, [schemaProperties, customColumns])

  const tableColumns = generateColumns()

  const table = useReactTable({
    data: data?.data ?? [],
    columns: tableColumns,
    state: {
      sorting,
      columnVisibility,
      rowSelection,
      columnFilters,
    },
    enableRowSelection: true,
    onRowSelectionChange: setRowSelection,
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onColumnVisibilityChange: setColumnVisibility,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFacetedRowModel: getFacetedRowModel(),
    getFacetedUniqueValues: getFacetedUniqueValues(),
  })

  return (
    <div className='space-y-4'>
      <div className='flex items-center justify-between'>
        <div className='flex flex-1 items-center space-x-2'>
          <Input
            placeholder='Filter...'
            value={(table.getColumn('name')?.getFilterValue() as string) ?? ''}
            onChange={(event) =>
              table.getColumn('name')?.setFilterValue(event.target.value)
            }
            className='h-8 w-[150px] lg:w-[250px]'
          />
          {columnFilters.length > 0 && (
            <Button
              variant='ghost'
              onClick={() => table.resetColumnFilters()}
              className='h-8 px-2 lg:px-3'
            >
              Reset
              <Cross2Icon className='ml-2 h-4 w-4' />
            </Button>
          )}
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant='outline'
              size='sm'
              className='ml-auto hidden h-8 lg:flex'
            >
              <MixerHorizontalIcon className='mr-2 h-4 w-4' />
              View
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align='end' className='w-[150px]'>
            <DropdownMenuLabel>Toggle columns</DropdownMenuLabel>
            <DropdownMenuSeparator />
            {table
              .getAllColumns()
              .filter(
                (column) =>
                  typeof column.accessorFn !== 'undefined' &&
                  column.getCanHide() &&
                  column.id !== 'actions'
              )
              .map((column) => {
                return (
                  <DropdownMenuCheckboxItem
                    key={column.id}
                    className='capitalize'
                    checked={column.getIsVisible()}
                    onCheckedChange={(value) => {
                      column.toggleVisibility(!!value)
                    }}
                    onSelect={(e) => {
                      e.preventDefault()
                    }}
                  >
                    {column.id}
                  </DropdownMenuCheckboxItem>
                )
              })}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>

      <div className='rounded-md border'>
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => (
                  <TableHead key={header.id}>
                    {header.isPlaceholder
                      ? null
                      : flexRender(
                          header.column.columnDef.header,
                          header.getContext()
                        )}
                  </TableHead>
                ))}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <TableCell
                  colSpan={tableColumns.length}
                  className='h-24 text-center'
                >
                  Loading...
                </TableCell>
              </TableRow>
            ) : table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <ContextMenu key={row.id}>
                  <ContextMenuTrigger asChild>
                    <TableRow
                      data-state={row.getIsSelected() && 'selected'}
                      className='cursor-context-menu'
                    >
                      {row.getVisibleCells().map((cell) => (
                        <TableCell key={cell.id}>
                          {flexRender(
                            cell.column.columnDef.cell,
                            cell.getContext()
                          )}
                        </TableCell>
                      ))}
                    </TableRow>
                  </ContextMenuTrigger>
                  {customColumns?.find(
                    (col): col is ActionColumn =>
                      typeof col === 'object' &&
                      'type' in col &&
                      col.type === 'action'
                  ) && (
                    <ContextMenuContent className='w-[160px]'>
                      {customColumns
                        .find(
                          (col): col is ActionColumn =>
                            typeof col === 'object' &&
                            'type' in col &&
                            col.type === 'action'
                        )
                        ?.actions.map((action, index) => (
                          <ContextMenuItem
                            key={index}
                            onClick={() => action.onClick(row.original)}
                          >
                            <action.Icon className='mr-2 h-4 w-4' />
                            {action.text}
                          </ContextMenuItem>
                        ))}
                    </ContextMenuContent>
                  )}
                </ContextMenu>
              ))
            ) : (
              <TableRow>
                <TableCell
                  colSpan={tableColumns.length}
                  className='h-24 text-center'
                >
                  No results.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      <div className='flex items-center justify-between px-2'>
        <div className='text-muted-foreground flex-1 text-sm'>
          {table.getFilteredSelectedRowModel().rows.length} of{' '}
          {table.getFilteredRowModel().rows.length} row(s) selected.
        </div>
        <div className='flex items-center space-x-6 lg:space-x-8'>
          <div className='flex items-center space-x-2'>
            <p className='text-sm font-medium'>Rows per page</p>
            <Select
              value={`${pageSize}`}
              onValueChange={(value) => {
                setPageSize(Number(value))
              }}
            >
              <SelectTrigger className='h-8 w-[70px]'>
                <SelectValue placeholder={pageSize} />
              </SelectTrigger>
              <SelectContent side='top'>
                {[10, 20, 30, 40, 50].map((size) => (
                  <SelectItem key={size} value={`${size}`}>
                    {size}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className='flex w-[100px] items-center justify-center text-sm font-medium'>
            Page {page} of {Math.ceil((data?.total ?? 0) / pageSize)}
          </div>
          <div className='flex items-center space-x-2'>
            <Button
              variant='outline'
              className='hidden h-8 w-8 p-0 lg:flex'
              onClick={() => setPage(1)}
              disabled={page === 1}
            >
              <span className='sr-only'>Go to first page</span>
              <ChevronLeftIcon className='h-4 w-4' />
            </Button>
            <Button
              variant='outline'
              className='h-8 w-8 p-0'
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
            >
              <span className='sr-only'>Go to previous page</span>
              <ChevronLeftIcon className='h-4 w-4' />
            </Button>
            <Button
              variant='outline'
              className='h-8 w-8 p-0'
              onClick={() => setPage((p) => p + 1)}
              disabled={page >= Math.ceil((data?.total ?? 0) / pageSize)}
            >
              <span className='sr-only'>Go to next page</span>
              <ChevronRightIcon className='h-4 w-4' />
            </Button>
            <Button
              variant='outline'
              className='hidden h-8 w-8 p-0 lg:flex'
              onClick={() => setPage(Math.ceil((data?.total ?? 0) / pageSize))}
              disabled={page >= Math.ceil((data?.total ?? 0) / pageSize)}
            >
              <span className='sr-only'>Go to last page</span>
              <ChevronRightIcon className='h-4 w-4' />
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
