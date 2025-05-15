import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { axiosInstance } from '@/lib/axios'
import { CommonDialog } from '@/components/ui/common-dialog'
import { DynamicForm } from '@/components/ui/dynamic-form'

const formSchema = z.object({
  siteId: z.number().optional(),
  siteUrl: z.string().min(1, 'Site URL is required'),
  color: z.string().optional(),
  name: z.string().min(1, 'Name is required'),
  note: z.string().optional(),
  groupId: z.number().default(1),
  tag: z.string().optional(),
  username: z.string().optional(),
  password: z.string().optional(),
  tfaSecret: z.string().optional(),
  cookie: z.string().optional(),
})

const fieldConfigs = [
  { name: 'name', label: 'Name', placeholder: 'Enter profile name' },
  { name: 'siteUrl', label: 'Site URL', placeholder: 'Enter site URL' },
  { name: 'color', label: 'Color', type: 'color' },
  { name: 'note', label: 'Note', textarea: true },
  { name: 'tag', label: 'Tag', placeholder: 'Enter tag' },
  { name: 'username', label: 'Username', placeholder: 'Enter username' },
  {
    name: 'password',
    label: 'Password',
    placeholder: 'Enter password',
    type: 'password',
  },
  { name: 'tfaSecret', label: '2FA Secret', placeholder: 'Enter 2FA secret' },
  { name: 'cookie', label: 'Cookie', textarea: true },
] as const

export function BrowserProfilesCreateDialog() {
  const queryClient = useQueryClient()

  const createProfileMutation = useMutation({
    mutationFn: async (data: any) => {
      await axiosInstance.post('/api/v1/browser-profiles/create', data)
    },
    onSuccess: () => {
      toast.success('Profile created successfully')
      queryClient.invalidateQueries({
        queryKey: ['/api/v1/browser-profiles/list'],
      })
    },
    onError: () => {
      toast.error('Failed to create profile')
    },
  })

  return (
    <CommonDialog
      stateKey='browserProfiles'
      dialogType='create'
      title='Create Browser Profile'
      description='Create a new browser profile by providing the necessary information.'
      onSubmit={() => {}} // handled by <form onSubmit>
      isSubmitting={createProfileMutation.isPending}
    >
      <DynamicForm
        schema={formSchema}
        defaultValues={{
          siteUrl: '',
          name: '',
          groupId: 1,
        }}
        fields={fieldConfigs}
        isSubmitting={createProfileMutation.isPending}
        onSubmit={(data) => createProfileMutation.mutate(data)}
      />
    </CommonDialog>
  )
}
