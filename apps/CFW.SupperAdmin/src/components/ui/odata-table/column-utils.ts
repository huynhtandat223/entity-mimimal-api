import { ColumnDef } from '@tanstack/react-table'

type ColumnDefinition<TData> =
  | keyof TData
  | ((row: TData) => React.ReactNode)
  | 'actions'

type ActionColumnConfig<TData> = {
  type: 'actions'
  render: (row: TData) => React.ReactNode
}

type ColumnConfig<TData> = {
  type: 'property' | 'computed'
  property: keyof TData
  title?: string
  render?: (row: TData) => React.ReactNode
}

export function createColumn<TData>(
  config: ColumnConfig<TData>
): ColumnDef<TData> {
  const column: ColumnDef<TData> = {
    id: String(config.property),
    accessorKey: config.property,
    header:
      config.title ||
      String(config.property).charAt(0).toUpperCase() +
        String(config.property).slice(1),
  }

  if (config.render) {
    column.cell = ({ row }) => config.render?.(row.original)
  }

  return column
}

export function createActionColumn<TData>(
  config: ActionColumnConfig<TData>
): ColumnDef<TData> {
  return {
    id: 'actions',
    header: 'Actions',
    cell: ({ row }) => config.render(row.original),
  }
}

export function createColumns<TData>(
  definitions: ColumnDefinition<TData>[]
): ColumnDef<TData>[] {
  return definitions.map((def): ColumnDef<TData> => {
    if (typeof def === 'string') {
      if (def === 'actions') {
        throw new Error(
          'Actions column must be defined using createActionColumn'
        )
      }
      return createColumn({ type: 'property', property: def as keyof TData })
    }

    // For function definitions, we need to infer the property name
    const propertyName = def
      .toString()
      .match(/\({\s*(\w+)\s*}/)?.[1] as keyof TData
    if (!propertyName) {
      throw new Error('Could not infer property name from function definition')
    }

    return createColumn({
      type: 'computed',
      property: propertyName,
      render: def as (row: TData) => React.ReactNode,
    })
  })
}
