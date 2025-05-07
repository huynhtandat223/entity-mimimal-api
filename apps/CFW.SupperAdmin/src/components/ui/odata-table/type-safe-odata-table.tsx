import React from 'react'
import { useQuery } from '@tanstack/react-query'
import {
  ColumnDef,
  ColumnFiltersState,
  PaginationState,
  SortingState,
  VisibilityState,
  flexRender,
  getCoreRowModel,
  getFacetedRowModel,
  getFacetedUniqueValues,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  useReactTable,
} from '@tanstack/react-table'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { DataTablePagination } from './data-table-pagination'
import { DataTableToolbar } from './data-table-toolbar'
import { ODataParams } from './types'

export interface FilterConfig {
  id: string
  title: string
  options: {
    value: string
    label: string
  }[]
}

type ColumnDefinition<TData> = keyof TData | ((row: TData) => React.ReactNode)

function createColumn<TData>(def: ColumnDefinition<TData>): ColumnDef<TData> {
  if (
    typeof def === 'string' ||
    typeof def === 'number' ||
    typeof def === 'symbol'
  ) {
    return {
      id: String(def),
      accessorKey: def,
      header: String(def).charAt(0).toUpperCase() + String(def).slice(1),
    }
  }

  if (typeof def === 'function') {
    // For functions, we'll use a unique ID and just the cell renderer
    const id = Math.random().toString(36).substring(7)
    return {
      id,
      header: id.charAt(0).toUpperCase() + id.slice(1),
      cell: ({ row }) => def(row.original),
    }
  }

  throw new Error('Invalid column definition')
}

interface TypeSafeODataTableProps<TData> {
  columns: ColumnDefinition<TData>[]
  queryFn: (
    params: Record<string, any>
  ) => Promise<{ value?: TData | TData[]; '@odata.count'?: number | null }>
  filters?: FilterConfig[]
  config?: {
    defaultPageSize?: number
    enableColumnSelection?: boolean
    enableFilters?: boolean
    enableSorting?: boolean
    enablePagination?: boolean
  }
}

export function TypeSafeODataTable<TData>({
  columns,
  queryFn,
  filters,
  config = {},
}: TypeSafeODataTableProps<TData>) {
  const {
    defaultPageSize = 10,
    enableColumnSelection = true,
    enableFilters = true,
    enableSorting = true,
    enablePagination = true,
  } = config

  const [sorting, setSorting] = React.useState<SortingState>([])
  const [columnFilters, setColumnFilters] = React.useState<ColumnFiltersState>(
    []
  )
  const [columnVisibility, setColumnVisibility] =
    React.useState<VisibilityState>({})
  const [rowSelection, setRowSelection] = React.useState({})
  const [pagination, setPagination] = React.useState<PaginationState>({
    pageIndex: 0,
    pageSize: defaultPageSize,
  })

  const columnDefs = React.useMemo(
    () => columns.map((def) => createColumn(def)),
    [columns]
  )

  const query = useQuery({
    queryKey: ['odata', { sorting, columnFilters, pagination }],
    queryFn: async () => {
      const apiParams = {
        $top: pagination.pageSize,
        $skip: pagination.pageIndex * pagination.pageSize,
        $orderby: sorting.length
          ? sorting
              .map((sort) => `${sort.id} ${sort.desc ? 'desc' : 'asc'}`)
              .join(',')
          : undefined,
        $filter: columnFilters.length
          ? columnFilters
              .map((filter) => `${filter.id} eq '${filter.value}'`)
              .join(' and ')
          : undefined,
        $count: true,
      }

      const response = await queryFn(apiParams)
      return {
        value: Array.isArray(response.value)
          ? response.value
          : response.value
            ? [response.value]
            : [],
        '@odata.count': response['@odata.count'],
      }
    },
  })

  const updateParams = (params: ODataParams) => {
    setPagination({
      pageIndex: params.pageIndex,
      pageSize: params.pageSize,
    })
    setSorting(params.sorting)
    setColumnFilters(params.columnFilters)
  }

  const table = useReactTable({
    data: query.data?.value ?? [],
    columns: columnDefs,
    state: {
      sorting,
      columnVisibility,
      rowSelection,
      columnFilters,
      pagination,
    },
    pageCount: Math.max(
      1,
      Math.ceil((query.data?.['@odata.count'] ?? 0) / pagination.pageSize)
    ),
    enableRowSelection: true,
    manualPagination: true,
    onRowSelectionChange: setRowSelection,
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onColumnVisibilityChange: setColumnVisibility,
    onPaginationChange: setPagination,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFacetedRowModel: getFacetedRowModel(),
    getFacetedUniqueValues: getFacetedUniqueValues(),
  })

  return (
    <div className='space-y-4'>
      {enableFilters && filters && (
        <DataTableToolbar table={table} filters={filters} />
      )}
      <div className='rounded-md border'>
        <Table>
          <TableHeader>
            {table.getHeaderGroups().map((headerGroup) => (
              <TableRow key={headerGroup.id}>
                {headerGroup.headers.map((header) => {
                  return (
                    <TableHead key={header.id} colSpan={header.colSpan}>
                      {header.isPlaceholder
                        ? null
                        : flexRender(
                            header.column.columnDef.header,
                            header.getContext()
                          )}
                    </TableHead>
                  )
                })}
              </TableRow>
            ))}
          </TableHeader>
          <TableBody>
            {query.isLoading ? (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className='h-24 text-center'
                >
                  Loading...
                </TableCell>
              </TableRow>
            ) : query.isError ? (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className='h-24 text-center'
                >
                  Error loading data. Please check API connection.
                </TableCell>
              </TableRow>
            ) : table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow
                  key={row.id}
                  data-state={row.getIsSelected() && 'selected'}
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
              ))
            ) : (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className='h-24 text-center'
                >
                  No results.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
      {enablePagination && (
        <DataTablePagination
          table={table}
          totalCount={query.data?.['@odata.count'] ?? 0}
          updateParams={updateParams}
        />
      )}
    </div>
  )
}
