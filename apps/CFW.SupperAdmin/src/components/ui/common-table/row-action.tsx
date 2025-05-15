// components/RowAction.tsx
import { DotsHorizontalIcon } from '@radix-ui/react-icons'
import { ExternalLink } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

type RowActionProps = {
  row: any
  onOpen?: (row: any) => void
}

export function RowAction({ row, onOpen }: RowActionProps) {
  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger asChild>
        <Button
          variant='ghost'
          className='data-[state=open]:bg-muted flex h-8 w-8 p-0'
        >
          <DotsHorizontalIcon className='h-4 w-4' />
          <span className='sr-only'>Open menu</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align='end' className='w-[160px]'>
        <DropdownMenuItem onClick={() => onOpen?.(row)}>
          <ExternalLink className='mr-2 h-4 w-4' />
          Open
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
