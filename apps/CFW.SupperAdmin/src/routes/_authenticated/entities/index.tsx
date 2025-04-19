import { createFileRoute } from '@tanstack/react-router'
import { DatabaseProvider } from '@/context/DatabaseContext'
import { ThemeProvider } from '@/context/theme-context'
import { DatabaseExplorer } from '@/components/database-explorer/DatabaseExplorer'
import { ModeToggle } from '@/components/theme/mode-toggle'

export const Route = createFileRoute('/_authenticated/entities/')({
  component: () => (
    <ThemeProvider defaultTheme='light' storageKey='db-explorer-theme'>
      <div className='absolute top-4 right-4 z-50'>
        <ModeToggle />
      </div>
      <DatabaseProvider>
        <DatabaseExplorer />
      </DatabaseProvider>
    </ThemeProvider>
  ),
})
