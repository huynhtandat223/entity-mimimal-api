import { ReactNode } from 'react'
import { LucideIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { ProfileDropdown } from '@/components/profile-dropdown'
import { Search } from '@/components/search'
import { ThemeSwitch } from '@/components/theme-switch'
import { Header } from './header'
import { Main } from './main'

interface PageButtonDef {
  text: string
  icon: LucideIcon
  onClick: () => void
}

interface PageLayoutProps {
  children: ReactNode
  title: string
  description?: string
  pageButtonDefs?: PageButtonDef[]
}

export function PageLayout({
  children,
  title,
  description,
  pageButtonDefs,
}: PageLayoutProps) {
  return (
    <>
      <Header fixed>
        <Search />
        <div className='ml-auto flex items-center space-x-4'>
          <ThemeSwitch />
          <ProfileDropdown />
        </div>
      </Header>

      <Main>
        <div className='mb-2 flex flex-wrap items-center justify-between space-y-2 gap-x-4'>
          <div>
            <h2 className='text-2xl font-bold tracking-tight'>{title}</h2>
            {description && (
              <p className='text-muted-foreground'>{description}</p>
            )}
          </div>
          {pageButtonDefs && (
            <div className='flex gap-2'>
              {pageButtonDefs.map((def, index) => (
                <Button key={index} className='space-x-1' onClick={def.onClick}>
                  <span>{def.text}</span> <def.icon size={18} />
                </Button>
              ))}
            </div>
          )}
        </div>
        <div className='-mx-4 flex-1 overflow-auto px-4 py-1 lg:flex-row lg:space-y-0 lg:space-x-12'>
          {children}
        </div>
      </Main>
    </>
  )
}
