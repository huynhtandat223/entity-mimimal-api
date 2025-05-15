import { ReactNode, useCallback, useEffect, useState } from 'react'
import React from 'react'
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

export interface ActionColumnDef {
  text: string
  Icon: LucideIcon
  onClick: (row: any) => void
}

export interface CommonTableProps {
  columns?: (
    | string
    | React.ReactElement
    | { type: 'action'; actions: ActionColumnDef[] }
  )[]
  apiUrl: string
  queryKey?: string[]
  schemaUrl: string
  schemaName: string
}

export function CommonTable({
  columns: customColumns = [],
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
        const schemaDef = schema.components?.schemas?.[schemaName]
        if (!schemaDef?.properties) return
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
    const columns: ColumnDef<any>[] = []
    for (const col of customColumns) {
      if (typeof col === 'string') {
        const prop = schemaProperties[col]
        if (!prop) continue
        columns.push({
          accessorKey: col,
          id: col,
          header:
            prop.description || col.charAt(0).toUpperCase() + col.slice(1),
          cell: ({ row }) => {
            const value = row.getValue(col)
            if (value === null || value === undefined) return '-'
            if (Array.isArray(value)) return value.join(', ')
            if (col === 'lastOpenTime' && typeof value === 'number') {
              return new Date(value).toLocaleString()
            }
            if (typeof value === 'boolean') return value ? 'Yes' : 'No'
            return String(value)
          },
        })
      } else if (React.isValidElement(col)) {
        const id = col.key ?? 'custom-' + columns.length
        columns.push({
          id: String(id),
          header: 'Actions',
          cell: ({ row }) => React.cloneElement(col, { row: row.original }),
        })
      }
    }
    return columns
  }, [schemaProperties, customColumns])

  const hasContextMenu = customColumns.some(
    (col) =>
      (typeof col === 'object' && 'type' in col && col.type === 'action') ||
      React.isValidElement(col)
  )

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
          ) : table.getRowModel().rows.length ? (
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

                {hasContextMenu && (
                  <ContextMenuContent className='w-[160px]'>
                    {/* Classic action definitions */}
                    {customColumns
                      .filter(
                        (
                          col
                        ): col is {
                          type: 'action'
                          actions: ActionColumnDef[]
                        } =>
                          typeof col === 'object' &&
                          'type' in col &&
                          col.type === 'action'
                      )
                      .flatMap((col) =>
                        col.actions.map((action, index) => (
                          <ContextMenuItem
                            key={index}
                            onClick={() => action.onClick(row.original)}
                          >
                            <action.Icon className='mr-2 h-4 w-4' />
                            {action.text}
                          </ContextMenuItem>
                        ))
                      )}

                    {/* JSX-based elements like <RowAction /> */}
                    {customColumns
                      .filter((col) => React.isValidElement(col))
                      .map((element, i) => {
                        const Comp = element as React.ReactElement<{ row: any }>
                        return (
                          <React.Fragment key={`jsx-${i}`}>
                            {React.cloneElement(Comp, { row: row.original })}
                          </React.Fragment>
                        )
                      })}
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
  )
}
