import { createFileRoute } from '@tanstack/react-router'
import { ColumnDef } from '@tanstack/react-table'
import { ODataTable } from '@/components/ui/odata-table'
import { DataTableColumnHeader } from '@/components/ui/data-table-column-header'

interface Tenant {
  id: string
  name: string
  description: string
  createdAt: string
}

const columns: ColumnDef<Tenant>[] = [
  {
    accessorKey: 'name',
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="Name" />
    ),
  },
  {
    accessorKey: 'description',
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="Description" />
    ),
  },
  {
    accessorKey: 'createdAt',
    header: ({ column }) => (
      <DataTableColumnHeader column={column} title="Created At" />
    ),
    cell: ({ row }) => {
      const date = new Date(row.getValue('createdAt'))
      return <div>{date.toLocaleDateString()}</div>
    },
  },
]

const filters = [
  {
    id: 'name',
    title: 'Name',
    options: [
      { value: 'tenant1', label: 'Tenant 1' },
      { value: 'tenant2', label: 'Tenant 2' },
    ],
  },
]

export const Route = createFileRoute('/_authenticated/tenants/')({
  component: () => (
    <div className="container mx-auto py-10">
      <ODataTable
        columns={columns}
        apiRoute="/api/tenants"
        filters={filters}
        config={{
          defaultPageSize: 10,
          enableColumnSelection: true,
          enableFilters: true,
          enableSorting: true,
          enablePagination: true,
        }}
      />
    </div>
  ),
})