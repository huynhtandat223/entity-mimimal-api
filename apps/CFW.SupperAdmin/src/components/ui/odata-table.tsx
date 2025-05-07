import React from 'react'
import {
  ColumnDef,
  ColumnFiltersState,
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
import qs from 'qs'
import { useQuery } from '@tanstack/react-query'
import { customAxios } from '@/lib/axios'

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { DataTablePagination } from '@/components/ui/data-table-pagination'
import { DataTableToolbar } from '@/components/ui/data-table-toolbar'

export interface FilterConfig {
  id: string
  title: string
  options: {
    value: string
    label: string
  }[]
}

interface ODataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[]
  apiRoute: string
  filters?: FilterConfig[]
  config?: {
    defaultPageSize?: number
    enableColumnSelection?: boolean
    enableFilters?: boolean
    enableSorting?: boolean
    enablePagination?: boolean
  }
}

export function ODataTable<TData, TValue>({
  columns,
  apiRoute,
  filters,
  config = {},
}: ODataTableProps<TData, TValue>) {
  const {
    defaultPageSize = 10,
    enableColumnSelection = true,
    enableFilters = true,
    enableSorting = true,
    enablePagination = true,
  } = config

  const [sorting, setSorting] = React.useState<SortingState>([])
  const [columnFilters, setColumnFilters] = React.useState<ColumnFiltersState>([])
  const [columnVisibility, setColumnVisibility] =
    React.useState<VisibilityState>({})
  const [rowSelection, setRowSelection] = React.useState({})

  // Build OData query
  const buildODataQuery = () => {
    const query: Record<string, any> = {
      $count: true,
      $top: defaultPageSize,
    }

    // Add sorting
    if (sorting.length > 0) {
      query.$orderby = sorting
        .map((sort) => `${sort.id} ${sort.desc ? 'desc' : 'asc'}`)
        .join(',')
    }

    // Add filtering
    if (columnFilters.length > 0) {
      const filterExpressions = columnFilters.map((filter) => {
        return `${filter.id} eq '${filter.value}'`
      })
      query.$filter = filterExpressions.join(' and ')
    }

    return qs.stringify(query, { addQueryPrefix: true })
  }

  // Fetch data
  const { data, isLoading, isError } = useQuery({
    queryKey: ['table-data', apiRoute, sorting, columnFilters, defaultPageSize],
    queryFn: async () => {
      const queryString = buildODataQuery()
      const response = await customAxios.get(`${apiRoute}${queryString}`)
      return response.data
    },
  })

  const table = useReactTable({
    data: data?.value ?? [],
    columns,
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
    <div className="space-y-4">
      {enableFilters && filters && (
        <DataTableToolbar
          table={table}
          filters={filters}
          enableColumnSelection={enableColumnSelection}
        />
      )}
      <div className="rounded-md border">
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
            {isLoading ? (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className="h-24 text-center"
                >
                  Loading...
                </TableCell>
              </TableRow>
            ) : isError ? (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className="h-24 text-center"
                >
                  Error loading data
                </TableCell>
              </TableRow>
            ) : table.getRowModel().rows?.length ? (
              table.getRowModel().rows.map((row) => (
                <TableRow
                  key={row.id}
                  data-state={row.getIsSelected() && "selected"}
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
                  className="h-24 text-center"
                >
                  No results.
                </TableCell>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>
      {enablePagination && (
        <DataTablePagination table={table} />
      )}
    </div>
  )
}