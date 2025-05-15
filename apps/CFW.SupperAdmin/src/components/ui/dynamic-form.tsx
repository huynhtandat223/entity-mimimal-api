// components/ui/dynamic-form.tsx
import { z, ZodRawShape, ZodObject } from 'zod'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  Form,
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'

interface FieldConfig {
  name: string
  label?: string
  placeholder?: string
  type?: string
  textarea?: boolean
}

interface DynamicFormProps<T extends ZodRawShape> {
  schema: ZodObject<T>
  defaultValues: Partial<z.infer<ZodObject<T>>>
  onSubmit: (values: z.infer<ZodObject<T>>) => void
  isSubmitting?: boolean
  fields: FieldConfig[]
}

export function DynamicForm<T extends ZodRawShape>({
  schema,
  defaultValues,
  onSubmit,
  isSubmitting,
  fields,
}: DynamicFormProps<T>) {
  const form = useForm<z.infer<typeof schema>>({
    resolver: zodResolver(schema),
    defaultValues,
  })

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className='space-y-4'>
        {fields.map(({ name, label, placeholder, type, textarea }) => (
          <FormField
            key={name}
            control={form.control}
            name={name as keyof z.infer<typeof schema>}
            render={({ field }) => (
              <FormItem>
                <FormLabel>{label}</FormLabel>
                <FormControl>
                  {textarea ? (
                    <Textarea placeholder={placeholder} {...field} />
                  ) : (
                    <Input
                      type={type || 'text'}
                      placeholder={placeholder}
                      {...field}
                    />
                  )}
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        ))}

        {/* optional footer can be moved into CommonDialog if needed */}
        {/* <DialogFooter>
          <Button type="submit" disabled={isSubmitting}>Submit</Button>
        </DialogFooter> */}
      </form>
    </Form>
  )
}
