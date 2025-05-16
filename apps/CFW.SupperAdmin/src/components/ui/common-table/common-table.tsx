import React, { useCallback, useEffect, useState } from 'react'
import { ExternalLinkIcon } from '@radix-ui/react-icons'
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
  type SortingState,
  type VisibilityState,
} from '@tanstack/react-table'
import { LucideIcon, MoreHorizontal } from 'lucide-react'
import { of, tap } from 'rxjs'
import { axiosInstance } from '@/lib/axios'
import { Button } from '@/components/ui/button'
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
} from '@/components/ui/context-menu'
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

interface ActionColumn {
  type: 'action'
  props: {
    actions: ActionColumnDef[]
  }
}

export interface CommonTableProps {
  columns?: (string | React.ReactElement | ActionColumn)[]
  apiUrl: string
  queryKey?: string[]
  schemaUrl: string
  schemaName: string
}

interface RowActionProps {
  row: any
  onOpen?: (row: any) => void
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
      } else if (
        typeof col === 'object' &&
        'type' in col &&
        col.type === 'action'
      ) {
        const actionCol = col as ActionColumn
        columns.push({
          id: 'actions',
          header: 'Actions',
          cell: ({ row }) => (
            <ContextMenu>
              <ContextMenuTrigger asChild>
                <Button variant='ghost' className='h-8 w-8 p-0'>
                  <MoreHorizontal className='h-4 w-4' />
                  <span className='sr-only'>Open menu</span>
                </Button>
              </ContextMenuTrigger>
              <ContextMenuContent className='w-[160px]'>
                {actionCol.props.actions.map((action, index) => {
                  return (
                    <ContextMenuItem
                      key={index}
                      onClick={() => action.onClick(row.original)}
                    >
                      <action.Icon className='mr-2 h-4 w-4' />
                      {action.text}
                    </ContextMenuItem>
                  )
                })}
              </ContextMenuContent>
            </ContextMenu>
          ),
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
                        (col): col is ActionColumn =>
                          typeof col === 'object' &&
                          'type' in col &&
                          col.type === 'action' &&
                          'props' in col &&
                          'actions' in (col as ActionColumn).props
                      )
                      .flatMap((col) =>
                        (col as ActionColumn).props.actions.map(
                          (action, index) => {
                            let rxJsOpration = of(row)

                            for (const operator of action.onClick) {
                              if (operator.operator === 'tap') {
                                const tapFunction = new Function(
                                  ...operator.args
                                )
                                rxJsOpration = rxJsOpration.pipe(
                                  tap(tapFunction)
                                )
                              }
                            }

                            const events = {
                              onClick: () => rxJsOpration.subscribe(),
                            }

                            return (
                              <ContextMenuItem key={index} {...events}>
                                <action.Icon className='mr-2 h-4 w-4' />
                                {action.text}
                              </ContextMenuItem>
                            )
                          }
                        )
                      )}

                    {/* JSX-based elements like <RowAction /> */}
                    {customColumns
                      .filter((col) => React.isValidElement(col))
                      .map((element, i) => {
                        const Comp =
                          element as React.ReactElement<RowActionProps>
                        const props = Comp.props
                        if (props.onOpen) {
                          return (
                            <ContextMenuItem
                              key={`jsx-${i}`}
                              onClick={() => props.onOpen?.(row.original)}
                            >
                              <ExternalLinkIcon className='mr-2 h-4 w-4' />
                              Open
                            </ContextMenuItem>
                          )
                        }
                        return null
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
