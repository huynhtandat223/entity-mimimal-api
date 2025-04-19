export type DatabaseType = 'postgresql' | 'mysql' | 'sqlserver' | 'oracle';

export interface ConnectionConfig {
  type: DatabaseType;
  connectionString: string;
  name?: string;
}

export interface ColumnInfo {
  name: string;
  type: string;
  nullable: boolean;
  isPrimaryKey: boolean;
  isForeignKey: boolean;
  defaultValue?: string;
  referencedTable?: string;
  referencedColumn?: string;
}

export interface TableInfo {
  name: string;
  schema: string;
  columns: ColumnInfo[];
  primaryKeys: string[];
  foreignKeys: {
    column: string;
    referencedTable: string;
    referencedColumn: string;
  }[];
}

export interface DatabaseMetadata {
  name: string;
  type: DatabaseType;
  tables: TableInfo[];
  schemas: string[];
}

export interface ApiConfig {
  routeName: string;
  selectedColumns: string[];
  relatedTables: {
    tableName: string;
    columns: string[];
  }[];
  authentication: boolean;
  authorization: {
    roles: string[];
    permissions: string[];
    policies: string[];
  };
  columnBehaviors: {
    columnName: string;
    format?: string;
    compute?: string;
  }[];
}

export interface AuthorizationMetadata {
  roles: string[];
  permissions: string[];
  policies: string[];
}