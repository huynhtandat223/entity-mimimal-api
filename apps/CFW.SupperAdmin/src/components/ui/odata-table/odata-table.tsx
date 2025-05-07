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
import qs from 'qs'
import { customAxiosFunction } from '@/lib/axios'
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
    dataProperty?: string
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
    dataProperty = 'value',
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
  const [totalCount, setTotalCount] = React.useState<number>(0)

  const buildODataQuery = () => {
    const query: Record<string, any> = {}

    // Only add pagination if pageSize is greater than 0
    if (pagination.pageSize > 0) {
      query.$top = pagination.pageSize
      query.$skip = pagination.pageIndex * pagination.pageSize
    }

    // Only add orderby if there are valid sorting values
    const orderby = sorting
      .map((sort) => `${sort.id} ${sort.desc ? 'desc' : 'asc'}`)
      .join(',')
      .trim()
    if (orderby) {
      query.$orderby = orderby
    }

    // Only add filter if there are valid filter values
    const filter = columnFilters
      .map((filter) => `${filter.id} eq '${filter.value}'`)
      .join(' and ')
      .trim()
    if (filter) {
      query.$filter = filter
    }

    // Only add $count if we have other parameters
    if (Object.keys(query).length > 0) {
      query.$count = true
    }

    // If no parameters, return empty string
    if (Object.keys(query).length === 0) {
      return ''
    }

    // Filter out any empty values before stringifying
    const filteredQuery = Object.fromEntries(
      Object.entries(query).filter(([_, value]) => {
        if (value === null || value === undefined) return false
        if (typeof value === 'string' && value.trim() === '') return false
        return true
      })
    )

    return qs.stringify(filteredQuery, { addQueryPrefix: true })
  }

  const getFullApiRoute = () => {
    const normalizedRoute = apiRoute.startsWith('/')
      ? apiRoute.substring(1)
      : apiRoute
    return normalizedRoute
  }

  const extractDataFromResponse = (response: any) => {
    let responseData = []
    if (response[dataProperty]) {
      responseData = response[dataProperty]
    } else if (Array.isArray(response)) {
      responseData = response
    } else if (response.data && Array.isArray(response.data)) {
      responseData = response.data
    }

    let totalItems = 0
    if (response['@odata.count'] !== undefined) {
      totalItems = response['@odata.count']
    } else if (response.count !== undefined) {
      totalItems = response.count
    } else if (response['@count'] !== undefined) {
      totalItems = response['@count']
    } else if (response.totalCount !== undefined) {
      totalItems = response.totalCount
    }

    if (totalItems === 0 && responseData.length > 0) {
      totalItems = responseData.length
    }

    return { data: responseData, count: totalItems }
  }

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['table-data', apiRoute, sorting, columnFilters, pagination],
    queryFn: async () => {
      try {
        const queryString = buildODataQuery()
        const fullRoute = getFullApiRoute()

        if (import.meta.env.DEV) {
          console.log(`Fetching data from: ${fullRoute}${queryString}`)
        }

        const response = await customAxiosFunction({
          method: 'GET',
          url: `${fullRoute}${queryString}`,
        })

        const extractedData = extractDataFromResponse(response)
        setTotalCount(extractedData.count)

        return { value: extractedData.data }
      } catch (err) {
        if (import.meta.env.DEV) {
          console.error('Error fetching data:', err)
        }
        throw err
      }
    },
  })

  React.useEffect(() => {
    if (isError && import.meta.env.DEV) {
      console.error('Error in ODataTable query:', error)
    }
  }, [isError, error])

  const table = useReactTable({
    data: data?.value ?? [],
    columns,
    state: {
      sorting,
      columnVisibility,
      rowSelection,
      columnFilters,
      pagination,
    },
    pageCount: Math.max(1, Math.ceil(totalCount / pagination.pageSize)),
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
            {isLoading ? (
              <TableRow>
                <TableCell
                  colSpan={columns.length}
                  className='h-24 text-center'
                >
                  Loading...
                </TableCell>
              </TableRow>
            ) : isError ? (
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
          totalCount={totalCount}
          updateParams={(params) => {
            setPagination({
              pageIndex: params.pageIndex,
              pageSize: params.pageSize,
            })
            setSorting(params.sorting)
            setColumnFilters(params.columnFilters)
          }}
        />
      )}
    </div>
  )
}
