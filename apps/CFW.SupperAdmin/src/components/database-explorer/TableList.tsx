import { useDatabase } from '@/context/DatabaseContext';
import { ScrollArea } from '@/components/ui/scroll-area';
import { Input } from '@/components/ui/input';
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from '@/components/ui/accordion';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { Badge } from '@/components/ui/badge';
import { useState } from 'react';
import { Search, Database, Table2 } from 'lucide-react';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';

export function TableList() {
  const { databaseMetadata, selectedTable, selectTable } = useDatabase();
  const [searchQuery, setSearchQuery] = useState('');
  const [schemaFilter, setSchemaFilter] = useState<string | null>(null);

  if (!databaseMetadata) return null;

  const { tables, schemas } = databaseMetadata;

  // Filter tables based on search query and schema filter
  const filteredTables = tables.filter(table => {
    const matchesSearch = table.name.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesSchema = schemaFilter === null || table.schema === schemaFilter;
    return matchesSearch && matchesSchema;
  });

  // Group tables by schema
  const tablesBySchema: Record<string, typeof tables> = {};
  filteredTables.forEach(table => {
    if (!tablesBySchema[table.schema]) {
      tablesBySchema[table.schema] = [];
    }
    tablesBySchema[table.schema].push(table);
  });

  return (
    <div className="h-full border-r flex flex-col">
      <div className="p-4 border-b">
        <div className="flex items-center space-x-2 mb-4">
          <Database className="h-5 w-5 text-primary" />
          <h2 className="text-lg font-medium">{databaseMetadata.name}</h2>
        </div>
        <div className="relative mb-4">
          <Search className="h-4 w-4 absolute left-2.5 top-2.5 text-muted-foreground" />
          <Input
            placeholder="Search tables..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="pl-8"
          />
        </div>
        <Select
          value={schemaFilter || 'all-schemas'}
          onValueChange={(value) => setSchemaFilter(value === 'all-schemas' ? null : value)}
        >
          <SelectTrigger className="w-full">
            <SelectValue placeholder="All schemas" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all-schemas">All schemas</SelectItem>
            {schemas.map((schema) => (
              <SelectItem key={schema} value={schema}>
                {schema}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <ScrollArea className="flex-1">
        <div className="p-2">
          <Accordion
            type="multiple"
            defaultValue={Object.keys(tablesBySchema)}
            className="w-full"
          >
            {Object.entries(tablesBySchema).map(([schema, schemaTables]) => (
              <AccordionItem
                key={schema}
                value={schema}
                className="border rounded-md mb-2"
              >
                <AccordionTrigger className="px-3 py-2 hover:no-underline">
                  <div className="flex items-center">
                    <span className="font-medium">{schema}</span>
                    <Badge variant="outline" className="ml-2">
                      {schemaTables.length}
                    </Badge>
                  </div>
                </AccordionTrigger>
                <AccordionContent className="pb-1">
                  <div className="space-y-1 px-1">
                    {schemaTables.map((table) => (
                      <Button
                        key={`${table.schema}.${table.name}`}
                        variant="ghost"
                        className={cn(
                          "w-full justify-start px-2 py-1.5 h-auto text-sm font-normal",
                          selectedTable === table.name && "bg-muted"
                        )}
                        onClick={() => selectTable(table.name)}
                      >
                        <Table2 className="h-4 w-4 mr-2 text-muted-foreground" />
                        {table.name}
                      </Button>
                    ))}
                  </div>
                </AccordionContent>
              </AccordionItem>
            ))}
          </Accordion>
        </div>
      </ScrollArea>
    </div>
  );
}