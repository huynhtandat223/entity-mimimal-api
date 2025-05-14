import React, { useState } from 'react'
import type { ProfileInfo } from '@/api/model'

type BrowserProfilesDialogType = 'create' | 'update' | 'delete'

interface BrowserProfilesContextType {
  open: BrowserProfilesDialogType | null
  setOpen: (str: BrowserProfilesDialogType | null) => void
  currentRow: ProfileInfo | null
  setCurrentRow: React.Dispatch<React.SetStateAction<ProfileInfo | null>>
}

const BrowserProfilesContext = React.createContext<BrowserProfilesContextType>({
  open: null,
  setOpen: () => {},
  currentRow: null,
  setCurrentRow: () => {},
})

export const useBrowserProfiles = () => React.useContext(BrowserProfilesContext)

interface Props {
  children: React.ReactNode
}

export default function BrowserProfilesProvider({ children }: Props) {
  const [open, setOpen] = useState<BrowserProfilesDialogType | null>(null)
  const [currentRow, setCurrentRow] = useState<ProfileInfo | null>(null)

  return (
    <BrowserProfilesContext.Provider
      value={{ open, setOpen, currentRow, setCurrentRow }}
    >
      {children}
    </BrowserProfilesContext.Provider>
  )
}
