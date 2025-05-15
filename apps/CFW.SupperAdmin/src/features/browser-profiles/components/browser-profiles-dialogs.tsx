import { useCommonState } from '@/stores/commonStateStore'
import { BrowserProfilesCreateDialog } from './browser-profiles-create-dialog'

export function BrowserProfilesDialogs() {
  const { open, setOpen } = useCommonState().getState('browserProfiles') // ✅ Use common state with key

  return (
    <>
      <BrowserProfilesCreateDialog
        key='profile-create'
        open={open === 'create'}
        onOpenChange={(isOpen) => {
          console.log('Dialog onOpenChange:', isOpen)
          setOpen(isOpen ? 'create' : null)
        }}
      />
    </>
  )
}
