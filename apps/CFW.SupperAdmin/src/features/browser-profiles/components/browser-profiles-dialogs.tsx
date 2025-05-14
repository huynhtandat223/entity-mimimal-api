import { useBrowserProfiles } from '../context/browser-profiles-context'
import { BrowserProfilesCreateDialog } from './browser-profiles-create-dialog'

export function BrowserProfilesDialogs() {
  const { open, setOpen } = useBrowserProfiles()

  console.log('Dialog state:', open)

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
