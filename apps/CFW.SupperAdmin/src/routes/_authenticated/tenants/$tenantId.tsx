import { createFileRoute } from '@tanstack/react-router'
import { useGetApiV1TenantsKey } from '@/api/tenants/tenants'

export const Route = createFileRoute('/_authenticated/tenants/$tenantId')({
  component: TenantDetailsPage,
})

function TenantDetailsPage() {
  const { tenantId } = Route.useParams()
  const {
    data: response,
    isLoading,
    isError,
    error,
  } = useGetApiV1TenantsKey(tenantId)

  if (isLoading) return <div>Loading...</div>
  if (isError)
    return (
      <div>
        Error: {error instanceof Error ? error.message : 'Unknown error'}
      </div>
    )
  if (!response?.value) return <div>Tenant not found</div>

  const tenant = response.value

  return (
    <div className='container mx-auto py-10'>
      <h1 className='mb-4 text-2xl font-bold'>{tenant.name}</h1>
      <div className='grid grid-cols-1 gap-4 md:grid-cols-2'>
        <div className='bg-card rounded-lg p-4'>
          <h2 className='mb-2 text-lg font-semibold'>Details</h2>
          <dl className='space-y-2'>
            <div>
              <dt className='text-muted-foreground text-sm font-medium'>ID</dt>
              <dd>{tenant.id}</dd>
            </div>
            <div>
              <dt className='text-muted-foreground text-sm font-medium'>
                Name
              </dt>
              <dd>{tenant.name}</dd>
            </div>
          </dl>
        </div>
        <div className='bg-card rounded-lg p-4'>
          <h2 className='mb-2 text-lg font-semibold'>Statistics</h2>
          <dl className='space-y-2'>
            <div>
              <dt className='text-muted-foreground text-sm font-medium'>
                Roles
              </dt>
              <dd>{tenant.roles?.length || 0}</dd>
            </div>
            <div>
              <dt className='text-muted-foreground text-sm font-medium'>
                Users
              </dt>
              <dd>{tenant.tenantUsers?.length || 0}</dd>
            </div>
          </dl>
        </div>
      </div>
    </div>
  )
}
