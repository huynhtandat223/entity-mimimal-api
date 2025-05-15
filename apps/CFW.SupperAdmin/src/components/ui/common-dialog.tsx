// src/components/ui/common-dialog.tsx
import { ReactNode } from 'react'
import { useCommonState } from '@/stores/commonStateStore'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
  DialogClose,
} from '@/components/ui/dialog'

interface CommonDialogProps {
  stateKey: string
  dialogType: string
  title: string
  description?: string
  onSubmit?: () => void
  isSubmitting?: boolean
  children: ReactNode
  footer?: ReactNode
}

export function CommonDialog({
  stateKey,
  dialogType,
  title,
  description,
  onSubmit,
  isSubmitting = false,
  children,
  footer,
}: CommonDialogProps) {
  const { open, setOpen } = useCommonState().getState(stateKey)

  const isOpen = open === dialogType

  const handleOpenChange = (value: boolean) => {
    setOpen(value ? dialogType : null)
  }

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
      <DialogContent className='sm:max-w-[425px]'>
        <DialogHeader className='text-left'>
          <DialogTitle>{title}</DialogTitle>
          {description && <DialogDescription>{description}</DialogDescription>}
        </DialogHeader>

        {children}

        {footer !== undefined ? (
          footer
        ) : (
          <DialogFooter>
            <DialogClose asChild>
              <Button type='button' variant='outline'>
                Cancel
              </Button>
            </DialogClose>
            {onSubmit && (
              <Button onClick={onSubmit} disabled={isSubmitting}>
                Submit
              </Button>
            )}
          </DialogFooter>
        )}
      </DialogContent>
    </Dialog>
  )
}
