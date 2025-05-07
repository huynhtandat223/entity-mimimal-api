import { createFileRoute } from '@tanstack/react-router'
import { GetApiV1Tenants200Value } from '@/api/model'
import { getApiV1Tenants } from '@/api/tenants/tenants'
import { TypeSafeODataTable } from '@/components/ui/odata-table'
import { PageLayout } from '@/components/layout/PageLayout'

export const Route = createFileRoute('/_authenticated/tenants/')({
  component: TenantsPage,
})

export default function TenantsPage() {
  return (
    <PageLayout
      title='Tenants'
      description='Manage your tenants and their settings'
    >
      <TypeSafeODataTable<GetApiV1Tenants200Value>
        columns={[
          'id',
          'name',
          ({ roles }) => roles?.length || 0,
          ({ tenantUsers }) => tenantUsers?.length || 0,
          (_row) => (
            <div className='flex justify-end gap-2'>
              <button
                className='text-blue-600 hover:text-blue-800'
                onClick={() => {
                  // Handle edit
                }}
              >
                Edit
              </button>
              <button
                className='text-red-600 hover:text-red-800'
                onClick={() => {
                  // Handle delete
                }}
              >
                Delete
              </button>
            </div>
          ),
        ]}
        queryFn={getApiV1Tenants}
      />
    </PageLayout>
  )
}
