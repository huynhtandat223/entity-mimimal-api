import { useMutation } from '@tanstack/react-query'
import { createFileRoute } from '@tanstack/react-router'
import { ExternalLink, Plus } from 'lucide-react'
import { toast } from 'sonner'
import type { ProfileInfo } from '@/api/model'
import { axiosInstance } from '@/lib/axios'
import { CommonTable } from '@/components/ui/common-table/common-table'
import { PageLayout } from '@/components/layout/PageLayout'
import { BrowserProfilesDialogs } from '@/features/browser-profiles/components/browser-profiles-dialogs'
import BrowserProfilesProvider, {
  useBrowserProfiles,
} from '@/features/browser-profiles/context/browser-profiles-context'

export const Route = createFileRoute('/_authenticated/browser-profiles/')({
  component: BrowserProfilesPage,
})

function BrowserProfilesContent() {
  const { setOpen } = useBrowserProfiles()
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
    <>
      <PageLayout
        title='Browser Profiles'
        description='Manage your browser profiles'
        pageButtonDefs={[
          {
            text: 'Create',
            icon: Plus,
            onClick: () => setOpen('create'),
          },
        ]}
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
      <BrowserProfilesDialogs />
    </>
  )
}

function BrowserProfilesPage() {
  return (
    <BrowserProfilesProvider>
      <BrowserProfilesContent />
    </BrowserProfilesProvider>
  )
}
