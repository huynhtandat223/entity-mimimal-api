import { z } from 'zod'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { axiosInstance } from '@/lib/axios'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'

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

type CreateProfileForm = z.infer<typeof formSchema>

interface Props {
  open: boolean
  onOpenChange: (open: boolean) => void
}

export function BrowserProfilesCreateDialog({ open, onOpenChange }: Props) {
  const queryClient = useQueryClient()
  const form = useForm<CreateProfileForm>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      siteUrl: '',
      name: '',
      groupId: 1,
    },
  })

  const createProfileMutation = useMutation({
    mutationFn: async (data: CreateProfileForm) => {
      await axiosInstance.post('/api/v1/browser-profiles/create', data)
    },
    onSuccess: () => {
      toast.success('Profile created successfully')
      onOpenChange(false)
      form.reset()
      // Invalidate and refetch the profiles list
      queryClient.invalidateQueries({
        queryKey: ['/api/v1/browser-profiles/list'],
      })
    },
    onError: () => {
      toast.error('Failed to create profile')
    },
  })

  const onSubmit = (data: CreateProfileForm) => {
    createProfileMutation.mutate(data)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className='sm:max-w-[425px]'>
        <DialogHeader className='text-left'>
          <DialogTitle>Create Browser Profile</DialogTitle>
          <DialogDescription>
            Create a new browser profile by providing the necessary information.
          </DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form
            id='create-profile-form'
            onSubmit={form.handleSubmit(onSubmit)}
            className='space-y-4'
          >
            <FormField
              control={form.control}
              name='name'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Name</FormLabel>
                  <FormControl>
                    <Input placeholder='Enter profile name' {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='siteUrl'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Site URL</FormLabel>
                  <FormControl>
                    <Input placeholder='Enter site URL' {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='color'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Color</FormLabel>
                  <FormControl>
                    <Input type='color' {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='note'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Note</FormLabel>
                  <FormControl>
                    <Textarea placeholder='Enter note' {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='tag'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Tag</FormLabel>
                  <FormControl>
                    <Input placeholder='Enter tag' {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='username'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Username</FormLabel>
                  <FormControl>
                    <Input placeholder='Enter username' {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='password'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Password</FormLabel>
                  <FormControl>
                    <Input
                      type='password'
                      placeholder='Enter password'
                      {...field}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='tfaSecret'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>2FA Secret</FormLabel>
                  <FormControl>
                    <Input placeholder='Enter 2FA secret' {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name='cookie'
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Cookie</FormLabel>
                  <FormControl>
                    <Textarea placeholder='Enter cookie' {...field} />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <DialogFooter>
              <DialogClose asChild>
                <Button type='button' variant='outline'>
                  Cancel
                </Button>
              </DialogClose>
              <Button type='submit'>Create Profile</Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
