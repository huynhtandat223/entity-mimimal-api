import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { DatabaseType, ConnectionConfig } from '@/types/database';
import { useDatabase } from '@/context/DatabaseContext';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { ResetIcon, LockClosedIcon } from '@radix-ui/react-icons';
import { Loader2 } from 'lucide-react';

const connectionSchema = z.object({
  type: z.enum(['postgresql', 'mysql', 'sqlserver', 'oracle'] as const),
  connectionString: z.string().min(1, 'Connection string is required'),
  name: z.string().optional(),
});

export type ConnectionFormValues = z.infer<typeof connectionSchema>;

const databaseTypes: { value: DatabaseType; label: string }[] = [
  { value: 'postgresql', label: 'PostgreSQL' },
  { value: 'mysql', label: 'MySQL' },
  { value: 'sqlserver', label: 'SQL Server' },
  { value: 'oracle', label: 'Oracle' },
];

export function ConnectionForm() {
  const { connect, isConnecting, error } = useDatabase();
  const [showPassword, setShowPassword] = useState(false);

  const form = useForm<ConnectionFormValues>({
    resolver: zodResolver(connectionSchema),
    defaultValues: {
      type: 'postgresql',
      connectionString: '',
      name: '',
    },
  });

  async function onSubmit(data: ConnectionFormValues) {
    const config: ConnectionConfig = {
      type: data.type,
      connectionString: data.connectionString,
      name: data.name || `${data.type} Database`,
    };
    await connect(config);
  }

  const getPlaceholder = (type: DatabaseType) => {
    switch (type) {
      case 'postgresql':
        return 'postgresql://username:password@localhost:5432/database';
      case 'mysql':
        return 'mysql://username:password@localhost:3306/database';
      case 'sqlserver':
        return 'Server=localhost;Database=database;User Id=username;Password=password;';
      case 'oracle':
        return 'username/password@localhost:1521/service';
      default:
        return 'Enter connection string...';
    }
  };

  const currentType = form.watch('type');

  return (
    <Card className="w-full max-w-2xl mx-auto">
      <CardHeader>
        <CardTitle className="text-2xl">Connect to Database</CardTitle>
        <CardDescription>
          Enter your database connection details to explore its structure
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
            <FormField
              control={form.control}
              name="type"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Database Type</FormLabel>
                  <Select onValueChange={field.onChange} defaultValue={field.value}>
                    <FormControl>
                      <SelectTrigger>
                        <SelectValue placeholder="Select database type" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {databaseTypes.map((type) => (
                        <SelectItem key={type.value} value={type.value}>
                          {type.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="name"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Connection Name (Optional)</FormLabel>
                  <FormControl>
                    <Input placeholder="My Database Connection" {...field} />
                  </FormControl>
                  <FormDescription>
                    A friendly name to identify this connection
                  </FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />

            <FormField
              control={form.control}
              name="connectionString"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Connection String</FormLabel>
                  <FormControl>
                    <div className="relative">
                      <Input
                        type={showPassword ? 'text' : 'password'}
                        placeholder={getPlaceholder(currentType)}
                        className="pr-10 font-mono text-sm"
                        {...field}
                      />
                      <button
                        type="button"
                        className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300"
                        onClick={() => setShowPassword(!showPassword)}
                      >
                        <LockClosedIcon className="h-4 w-4" />
                      </button>
                    </div>
                  </FormControl>
                  <FormDescription>
                    The connection string for your {databaseTypes.find(t => t.value === currentType)?.label} database
                  </FormDescription>
                  <FormMessage />
                </FormItem>
              )}
            />

            {error && (
              <div className="bg-destructive/15 p-3 rounded-md text-destructive text-sm">
                {error}
              </div>
            )}

            <div className="flex justify-end space-x-4">
              <Button
                type="button"
                variant="outline"
                onClick={() => form.reset()}
                disabled={isConnecting}
              >
                <ResetIcon className="mr-2 h-4 w-4" />
                Reset
              </Button>
              <Button type="submit" disabled={isConnecting}>
                {isConnecting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {isConnecting ? 'Connecting...' : 'Connect'}
              </Button>
            </div>
          </form>
        </Form>
      </CardContent>
      <CardFooter className="flex justify-center border-t pt-6">
        <p className="text-sm text-muted-foreground">
          For demo purposes, any connection string will work. Data shown is sample data.
        </p>
      </CardFooter>
    </Card>
  );
}