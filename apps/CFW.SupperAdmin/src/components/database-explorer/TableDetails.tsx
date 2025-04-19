import { useState } from 'react';
import { useDatabase } from '@/context/DatabaseContext';
import { TableInfo, ApiConfig, AuthorizationMetadata } from '@/types/database';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Key, Link as LinkIcon, Database, ArrowLeft, Code2 } from 'lucide-react';
import { DataTable } from './data-table';
import { columns } from './columns';
import { Button } from '@/components/ui/button';
import { CreateApiDialog } from './create-api-dialog';
import { useToast } from '@/hooks/use-toast';

// Mock authorization metadata
const mockAuthMetadata: AuthorizationMetadata = {
  roles: ['admin', 'user', 'editor', 'viewer'],
  permissions: ['create', 'read', 'update', 'delete', 'publish', 'archive'],
  policies: ['public_access', 'authenticated_only', 'admin_only', 'own_records_only']
};

export function TableDetails() {
  const { databaseMetadata, selectedTable, disconnect } = useDatabase();
  const [isApiDialogOpen, setIsApiDialogOpen] = useState(false);
  const { toast } = useToast();

  if (!databaseMetadata || !selectedTable) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="text-center p-6">
          <Database className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
          <h3 className="text-lg font-medium">No table selected</h3>
          <p className="text-muted-foreground mt-2">
            Select a table from the sidebar to view its details
          </p>
        </div>
      </div>
    );
  }

  const tableInfo = databaseMetadata.tables.find((t) => t.name === selectedTable);

  if (!tableInfo) {
    return <div>Table not found</div>;
  }

  // Get foreign key relationships for this table
  const referencedTables = databaseMetadata.tables.filter((t) => 
    t.foreignKeys.some((fk) => fk.referencedTable === selectedTable)
  );

  const handleApiSubmit = (config: ApiConfig) => {
    console.log('API Configuration:', config);
    toast({
      title: "API Endpoint Created",
      description: `Successfully created API endpoint at ${config.routeName}`,
    });
    setIsApiDialogOpen(false);
  };

  return (
    <div className="h-full p-6 overflow-auto space-y-6">
      <div className="flex items-center justify-between">
        <Button variant="ghost" onClick={disconnect} className="gap-2">
          <ArrowLeft className="h-4 w-4" />
          Back to Connection
        </Button>
        <Button onClick={() => setIsApiDialogOpen(true)} className="gap-2">
          <Code2 className="h-4 w-4" />
          Create API
        </Button>
      </div>

      <div className="space-y-6">
        <DataTable columns={columns} data={tableInfo.columns} />

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
          {tableInfo.foreignKeys.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">
                  <div className="flex items-center">
                    <LinkIcon className="h-4 w-4 mr-2 text-purple-500" />
                    Outgoing Relationships
                  </div>
                </CardTitle>
                <CardDescription>
                  Foreign keys in this table that reference other tables
                </CardDescription>
              </CardHeader>
              <CardContent>
                <ul className="space-y-2">
                  {tableInfo.foreignKeys.map((fk, index) => (
                    <li key={index} className="p-2 rounded-md border bg-background">
                      <div className="flex items-center text-sm">
                        <span className="font-medium">{fk.column}</span>
                        <span className="mx-2 text-muted-foreground">references</span>
                        <Badge className="mr-1 bg-muted text-foreground">
                          {fk.referencedTable}
                        </Badge>
                        <span className="text-muted-foreground">({fk.referencedColumn})</span>
                      </div>
                    </li>
                  ))}
                </ul>
              </CardContent>
            </Card>
          )}

          {referencedTables.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">
                  <div className="flex items-center">
                    <LinkIcon className="h-4 w-4 mr-2 text-blue-500" />
                    Incoming Relationships
                  </div>
                </CardTitle>
                <CardDescription>
                  Foreign keys in other tables that reference this table
                </CardDescription>
              </CardHeader>
              <CardContent>
                <ul className="space-y-2">
                  {referencedTables.map((table) => 
                    table.foreignKeys
                      .filter(fk => fk.referencedTable === selectedTable)
                      .map((fk, index) => (
                        <li key={`${table.name}-${index}`} className="p-2 rounded-md border bg-background">
                          <div className="flex items-center text-sm">
                            <Badge className="mr-1 bg-muted text-foreground">
                              {table.name}
                            </Badge>
                            <span className="mx-2 text-muted-foreground">({fk.column})</span>
                            <span className="text-muted-foreground">references</span>
                            <span className="ml-2 font-medium">{fk.referencedColumn}</span>
                          </div>
                        </li>
                      ))
                  )}
                </ul>
              </CardContent>
            </Card>
          )}
        </div>
        
        {tableInfo.foreignKeys.length === 0 && referencedTables.length === 0 && (
          <div className="text-center p-6 border rounded-lg">
            <LinkIcon className="h-8 w-8 text-muted-foreground mx-auto mb-2" />
            <h3 className="text-lg font-medium">No relationships found</h3>
            <p className="text-muted-foreground mt-2">
              This table doesn't have any foreign key relationships
            </p>
          </div>
        )}
      </div>

      <CreateApiDialog
        open={isApiDialogOpen}
        onOpenChange={setIsApiDialogOpen}
        table={tableInfo}
        relatedTables={referencedTables}
        authMetadata={mockAuthMetadata}
        onSubmit={handleApiSubmit}
      />
    </div>
  );
}