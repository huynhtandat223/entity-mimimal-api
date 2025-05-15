import { ReactNode } from 'react'
import { useCommonState } from '@/stores/commonStateStore'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent } from '@/components/ui/dialog'
import { globalComponentRegistry } from '../component-schemas'

interface ButtonDialogProps {
  dialogKey: string
  dialogType?: string
  label: string
  icon?: string
  children?: ReactNode // already rendered from ComponentSchemas
}

export function ButtonDialog({
  dialogKey,
  dialogType = 'create',
  label,
  icon,
  children,
}: ButtonDialogProps) {
  const { open, setOpen } = useCommonState().getState(dialogKey)
  const isOpen = open === dialogType

  const IconComp = icon && globalComponentRegistry[icon]

  return (
    <>
      <Button onClick={() => setOpen(dialogType)} className='space-x-1'>
        <span>{label}</span>
        {IconComp && <IconComp size={18} />}
      </Button>

      <Dialog
        open={isOpen}
        onOpenChange={(v) => setOpen(v ? dialogType : null)}
      >
        <DialogContent className='sm:max-w-[425px]'>{children}</DialogContent>
      </Dialog>
    </>
  )
}
