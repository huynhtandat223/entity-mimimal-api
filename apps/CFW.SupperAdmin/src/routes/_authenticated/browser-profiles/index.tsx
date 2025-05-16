import { useMutation } from '@tanstack/react-query'
import { createFileRoute } from '@tanstack/react-router'
import { toast } from 'sonner'
import type { ProfileInfo } from '@/api/model'
import { axiosInstance } from '@/lib/axios'
import { ComponentSchemas } from '@/components/component-schemas'

export const Route = createFileRoute('/_authenticated/browser-profiles/')({
  component: BrowserProfilesContent,
})

function BrowserProfilesContent() {
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

  const deleteProfileMutation = useMutation({
    mutationFn: async (profileId: number) => {
      await axiosInstance.delete(`/api/v1/browser-profiles/${profileId}`)
    },
    onSuccess: () => {
      toast.success('Profile deleted successfully')
    },
    onError: () => {
      toast.error('Failed to delete profile')
    },
  })

  const handleOpenProfile = async (row: ProfileInfo) => {
    if (row.profileId) {
      openProfileMutation.mutate(row.profileId)
    }
  }

  const handleDeleteProfile = async (row: ProfileInfo) => {
    if (row.profileId) {
      deleteProfileMutation.mutate(row.profileId)
    }
  }

  const schema = {
    type: 'PageLayout',
    props: {
      title: 'Browser Profiles',
      description: 'Manage your browser profiles',
      pageButtonDefs: [
        {
          type: 'ButtonDialog',
          props: {
            dialogKey: 'browserProfiles',
            dialogType: 'create',
            label: 'Create',
            icon: 'Plus',
            children: {
              type: 'BrowserProfilesCreateDialog',
              props: {},
            },
          },
        },
      ],
    },
    children: {
      type: 'CommonTable',
      props: {
        apiUrl: '/api/v1/browser-profiles/list',
        schemaUrl: '/openapi/v1.json',
        schemaName: 'ProfileInfo',
        columns: [
          'id',
          'name',
          {
            type: 'action',
            props: {
              actions: [
                {
                  text: 'Open',
                  Icon: 'Play',
                  onClick: {
                    type: 'Function',
                    args: ['row', 'console.log(row)'],
                  },
                },
                {
                  text: 'Delete',
                  Icon: 'Trash',
                  onClick: {
                    type: 'Function',
                    args: ['row', 'console.log(row)'],
                  },
                },
              ],
            },
          },
        ],
      },
    },
  }

  return (
    <>
      <ComponentSchemas schema={schema} />
    </>
  )
}
