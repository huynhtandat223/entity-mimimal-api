import { useDatabase } from '@/context/DatabaseContext';
import { ConnectionForm } from './ConnectionForm';
import { TableList } from './TableList';
import { TableDetails } from './TableDetails';
import { Button } from '@/components/ui/button';
import { Loader2, Database } from 'lucide-react';

export function DatabaseExplorer() {
  const { isConnected, isConnecting, disconnect, databaseMetadata } = useDatabase();

  if (isConnecting) {
    return (
      <div className="flex items-center justify-center h-screen">
        <div className="text-center">
          <Loader2 className="h-8 w-8 animate-spin mx-auto mb-4 text-primary" />
          <h2 className="text-lg font-medium mb-2">Connecting to Database</h2>
          <p className="text-muted-foreground">Please wait while we establish the connection...</p>
        </div>
      </div>
    );
  }

  if (!isConnected) {
    return (
      <div className="container mx-auto py-8 px-4">
        <div className="flex flex-col items-center mb-8 text-center">
          <div className="p-4 bg-primary/10 rounded-full mb-4">
            <Database className="h-10 w-10 text-primary" />
          </div>
          <h1 className="text-3xl font-bold mb-2">Database Explorer</h1>
          <p className="text-muted-foreground max-w-xl">
            Connect to your database to explore its structure, tables, and relationships.
            Visualize schemas, tables, and their columns to better understand your data.
          </p>
        </div>
        <ConnectionForm />
      </div>
    );
  }

  return (
    <div className="h-screen flex flex-col">
      <header className="h-14 border-b flex items-center justify-between px-4">
        <div className="flex items-center">
          <Database className="h-5 w-5 mr-2 text-primary" />
          <h1 className="font-medium">Database Explorer</h1>
          {databaseMetadata && (
            <div className="ml-4 flex items-center">
              <span className="text-muted-foreground">Connected to</span>
              <span className="ml-1 font-medium">{databaseMetadata.name}</span>
            </div>
          )}
        </div>
        
        <Button variant="outline" size="sm" onClick={disconnect}>
          Disconnect
        </Button>
      </header>
      
      <div className="flex-1 grid grid-cols-[280px_1fr] overflow-hidden">
        <TableList />
        <TableDetails />
      </div>
    </div>
  );
}