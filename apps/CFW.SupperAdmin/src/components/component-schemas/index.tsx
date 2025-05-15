import React from 'react'
import { Plus, ExternalLink } from 'lucide-react'
import { isValidElementType } from 'react-is'
import { Button } from '@/components/ui/button'
import { CommonTable } from '@/components/ui/common-table/common-table'
import { PageLayout } from '@/components/layout/PageLayout'
import { BrowserProfilesCreateDialog } from '@/features/browser-profiles/components/browser-profiles-create-dialog'
import { ButtonDialog } from '../ui/button-dialog'
import { RowAction } from '../ui/common-table/row-action'

export type ComponentSchema = {
  type: string
  props?: Record<string, any>
  children?: ComponentSchema[] | ComponentSchema | string
}

type ComponentSchemasProps = {
  schema: ComponentSchema
  functions?: Record<string, (...args: any[]) => any>
}

export const globalComponentRegistry: Record<string, React.ElementType> = {
  Button,
  PageLayout,
  CommonTable,
  Plus,
  ExternalLink,
  span: 'span',
  div: 'div',
  ButtonDialog,
  BrowserProfilesCreateDialog,
  RowAction,
}

export function ComponentSchemas({
  schema,
  functions = {},
}: ComponentSchemasProps) {
  function renderNode(node: any): React.ReactNode {
    if (typeof node === 'string') return node

    const Comp =
      globalComponentRegistry[node.type] ||
      (node.type === 'Fragment' ? React.Fragment : node.type)

    if (!isValidElementType(Comp)) {
      console.warn(`Unknown or invalid component: ${node.type}`, Comp)
      return <div>Unknown or invalid component: {node.type}</div>
    }

    // Parse props (exclude 'children')
    const parsedProps = Object.fromEntries(
      Object.entries(node.props || {})
        .filter(([key]) => key !== 'children')
        .map(([key, value]) => {
          // Function handlers
          if (typeof value === 'string' && functions[value]) {
            return [key, functions[value]]
          }

          // Icon handling
          if (['icon', 'Icon', 'startIcon', 'endIcon'].includes(key)) {
            if (typeof value === 'string' && globalComponentRegistry[value]) {
              return [key, globalComponentRegistry[value]]
            }
            if (typeof value === 'object' && value?.type) {
              const IconComp = globalComponentRegistry[value.type]
              if (IconComp) {
                return [
                  key,
                  (props: any) => <IconComp {...value.props} {...props} />,
                ]
              }
            }
          }

          // Nested component
          if (typeof value === 'object' && value?.type) {
            return [key, renderNode(value)]
          }

          // Array of nested components
          if (Array.isArray(value)) {
            return [
              key,
              value.map((v) =>
                typeof v === 'object' && v?.type ? renderNode(v) : v
              ),
            ]
          }

          return [key, value]
        })
    )

    // Handle children from props or top-level schema
    const rawChildren = node.props?.children ?? node.children

    const children = Array.isArray(rawChildren)
      ? rawChildren.map((child, i) => (
          <React.Fragment key={i}>{renderNode(child)}</React.Fragment>
        ))
      : typeof rawChildren === 'object' && rawChildren?.type
        ? renderNode(rawChildren)
        : rawChildren

    return <Comp {...parsedProps}>{children}</Comp>
  }

  return <>{renderNode(schema)}</>
}
