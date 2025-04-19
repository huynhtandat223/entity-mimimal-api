import { ColumnDef } from "@tanstack/react-table"
import { ColumnInfo } from "@/types/database"
import { Badge } from "@/components/ui/badge"
import { ArrowUpDown } from "lucide-react"
import { Button } from "@/components/ui/button"

export const columns: ColumnDef<ColumnInfo>[] = [
  {
    accessorKey: "name",
    header: ({ column }) => {
      return (
        <Button
          variant="ghost"
          onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
        >
          Name
          <ArrowUpDown className="ml-2 h-4 w-4" />
        </Button>
      )
    },
  },
  {
    accessorKey: "type",
    header: "Type",
    cell: ({ row }) => (
      <code className="px-1 py-0.5 bg-muted rounded text-sm">
        {row.getValue("type")}
      </code>
    ),
  },
  {
    accessorKey: "nullable",
    header: "Nullable",
    cell: ({ row }) => (
      row.getValue("nullable") ? (
        <Badge variant="outline">NULL</Badge>
      ) : (
        <Badge variant="secondary">NOT NULL</Badge>
      )
    ),
  },
  {
    accessorKey: "isPrimaryKey",
    header: "Primary Key",
    cell: ({ row }) => (
      row.getValue("isPrimaryKey") ? (
        <Badge className="bg-blue-500/20 text-blue-700 dark:bg-blue-500/30 dark:text-blue-300 border-blue-500/30">
          Yes
        </Badge>
      ) : (
        "No"
      )
    ),
  },
  {
    accessorKey: "isForeignKey",
    header: "Foreign Key",
    cell: ({ row }) => (
      row.getValue("isForeignKey") ? (
        <Badge className="bg-purple-500/20 text-purple-700 dark:bg-purple-500/30 dark:text-purple-300 border-purple-500/30">
          Yes
        </Badge>
      ) : (
        "No"
      )
    ),
  },
  {
    accessorKey: "defaultValue",
    header: "Default Value",
    cell: ({ row }) => {
      const value = row.getValue("defaultValue")
      return value ? (
        <code className="text-xs px-1 py-0.5 bg-muted rounded">
          {value as string}
        </code>
      ) : (
        <span className="text-muted-foreground italic">none</span>
      )
    },
  },
]