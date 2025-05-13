import { useMutation } from '@tanstack/react-query'
import { createFileRoute } from '@tanstack/react-router'
import { ExternalLink } from 'lucide-react'
import { toast } from 'sonner'
import type { ProfileInfo } from '@/api/model'
import { axiosInstance } from '@/lib/axios'
import { CommonTable } from '@/components/ui/common-table/common-table'
import { PageLayout } from '@/components/layout/PageLayout'

export const Route = createFileRoute('/_authenticated/browser-profiles/')({
  component: BrowserProfilesPage,
})

function BrowserProfilesPage() {
  const openProfileMutation = useMutation({
    mutationFn: async (profileId: number) => {
      await axiosInstance.post(`/api/v1/browser-profiles/open`, {
        profileId,
      })
    },
    onSuccess: () => {
      toast.success('Profile opened successfully')
    },
    onError: () => {
      toast.error('Failed to open profile')
    },
  })

  const handleOpenProfile = async (row: ProfileInfo) => {
    if (row.profileId) {
      openProfileMutation.mutate(row.profileId)
    }
  }

  return (
    <PageLayout
      title='Browser Profiles'
      description='Manage your browser profiles'
    >
      <CommonTable
        apiUrl='/api/v1/browser-profiles/list'
        schemaUrl='/openapi/v1.json'
        schemaName='ProfileInfo'
        columns={[
          {
            type: 'action',
            actions: [
              {
                text: 'Open',
                Icon: ExternalLink,
                onClick: handleOpenProfile,
              },
            ],
          },
        ]}
      />
    </PageLayout>
  )
}
