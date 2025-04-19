import { DatabaseMetadata, DatabaseType, TableInfo } from '@/types/database'

// Generate realistic column data types based on database type
function getDataTypes(dbType: DatabaseType): string[] {
  switch (dbType) {
    case 'postgresql':
      return [
        'integer',
        'text',
        'varchar',
        'timestamp',
        'boolean',
        'uuid',
        'jsonb',
        'numeric',
        'date',
      ]
    case 'mysql':
      return [
        'int',
        'varchar',
        'text',
        'datetime',
        'tinyint',
        'decimal',
        'float',
        'date',
        'enum',
      ]
    case 'sqlserver':
      return [
        'int',
        'nvarchar',
        'varchar',
        'datetime',
        'bit',
        'decimal',
        'float',
        'date',
        'uniqueidentifier',
      ]
    case 'oracle':
      return [
        'number',
        'varchar2',
        'date',
        'timestamp',
        'clob',
        'blob',
        'char',
        'float',
        'raw',
      ]
    default:
      return ['integer', 'text', 'date', 'boolean']
  }
}

// Generate realistic database schemas based on database type
function getSchemas(dbType: DatabaseType): string[] {
  switch (dbType) {
    case 'postgresql':
      return ['public', 'auth', 'analytics']
    case 'sqlserver':
      return ['dbo', 'sales', 'hr']
    case 'oracle':
      return ['HR', 'SALES', 'FINANCE']
    case 'mysql':
      // MySQL doesn't use schemas the same way, but often uses different databases instead
      return ['main']
    default:
      return ['public']
  }
}

// Generate sample tables with realistic names and structures
function generateTables(dbType: DatabaseType): TableInfo[] {
  const dataTypes = getDataTypes(dbType)
  const schemas = getSchemas(dbType)

  const tables: TableInfo[] = []

  // Users table
  const usersTable: TableInfo = {
    name: 'users',
    schema: schemas[0],
    columns: [
      {
        name: 'id',
        type:
          dbType === 'postgresql'
            ? 'uuid'
            : dbType === 'oracle'
              ? 'number'
              : 'int',
        nullable: false,
        isPrimaryKey: true,
        isForeignKey: false,
        defaultValue: dbType === 'postgresql' ? 'gen_random_uuid()' : null,
      },
      {
        name: 'email',
        type:
          dbType === 'sqlserver'
            ? 'nvarchar(255)'
            : dbType === 'oracle'
              ? 'varchar2(255)'
              : 'varchar(255)',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
      },
      {
        name: 'username',
        type:
          dbType === 'sqlserver'
            ? 'nvarchar(100)'
            : dbType === 'oracle'
              ? 'varchar2(100)'
              : 'varchar(100)',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
      },
      {
        name: 'password_hash',
        type:
          dbType === 'sqlserver'
            ? 'nvarchar(255)'
            : dbType === 'oracle'
              ? 'varchar2(255)'
              : 'varchar(255)',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
      },
      {
        name: 'created_at',
        type:
          dbType === 'postgresql'
            ? 'timestamp with time zone'
            : dbType === 'mysql'
              ? 'datetime'
              : 'timestamp',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
        defaultValue: dbType === 'postgresql' ? 'now()' : 'CURRENT_TIMESTAMP',
      },
      {
        name: 'updated_at',
        type:
          dbType === 'postgresql'
            ? 'timestamp with time zone'
            : dbType === 'mysql'
              ? 'datetime'
              : 'timestamp',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
        defaultValue: dbType === 'postgresql' ? 'now()' : 'CURRENT_TIMESTAMP',
      },
    ],
    primaryKeys: ['id'],
    foreignKeys: [],
  }
  tables.push(usersTable)

  // Products table
  const productsTable: TableInfo = {
    name: 'products',
    schema: schemas[0],
    columns: [
      {
        name: 'id',
        type:
          dbType === 'postgresql'
            ? 'uuid'
            : dbType === 'oracle'
              ? 'number'
              : 'int',
        nullable: false,
        isPrimaryKey: true,
        isForeignKey: false,
        defaultValue: dbType === 'postgresql' ? 'gen_random_uuid()' : null,
      },
      {
        name: 'name',
        type:
          dbType === 'sqlserver'
            ? 'nvarchar(255)'
            : dbType === 'oracle'
              ? 'varchar2(255)'
              : 'varchar(255)',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
      },
      {
        name: 'description',
        type:
          dbType === 'sqlserver'
            ? 'nvarchar(max)'
            : dbType === 'oracle'
              ? 'clob'
              : 'text',
        nullable: true,
        isPrimaryKey: false,
        isForeignKey: false,
      },
      {
        name: 'price',
        type:
          dbType === 'postgresql'
            ? 'numeric(10,2)'
            : dbType === 'mysql'
              ? 'decimal(10,2)'
              : 'decimal(10,2)',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
      },
      {
        name: 'category_id',
        type:
          dbType === 'postgresql'
            ? 'uuid'
            : dbType === 'oracle'
              ? 'number'
              : 'int',
        nullable: true,
        isPrimaryKey: false,
        isForeignKey: true,
        referencedTable: 'categories',
        referencedColumn: 'id',
      },
      {
        name: 'created_at',
        type:
          dbType === 'postgresql'
            ? 'timestamp with time zone'
            : dbType === 'mysql'
              ? 'datetime'
              : 'timestamp',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
        defaultValue: dbType === 'postgresql' ? 'now()' : 'CURRENT_TIMESTAMP',
      },
    ],
    primaryKeys: ['id'],
    foreignKeys: [
      {
        column: 'category_id',
        referencedTable: 'categories',
        referencedColumn: 'id',
      },
    ],
  }
  tables.push(productsTable)

  // Categories table
  const categoriesTable: TableInfo = {
    name: 'categories',
    schema: schemas[0],
    columns: [
      {
        name: 'id',
        type:
          dbType === 'postgresql'
            ? 'uuid'
            : dbType === 'oracle'
              ? 'number'
              : 'int',
        nullable: false,
        isPrimaryKey: true,
        isForeignKey: false,
        defaultValue: dbType === 'postgresql' ? 'gen_random_uuid()' : null,
      },
      {
        name: 'name',
        type:
          dbType === 'sqlserver'
            ? 'nvarchar(100)'
            : dbType === 'oracle'
              ? 'varchar2(100)'
              : 'varchar(100)',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
      },
      {
        name: 'description',
        type:
          dbType === 'sqlserver'
            ? 'nvarchar(max)'
            : dbType === 'oracle'
              ? 'clob'
              : 'text',
        nullable: true,
        isPrimaryKey: false,
        isForeignKey: false,
      },
    ],
    primaryKeys: ['id'],
    foreignKeys: [],
  }
  tables.push(categoriesTable)

  // Orders table
  const ordersTable: TableInfo = {
    name: 'orders',
    schema: schemas[0],
    columns: [
      {
        name: 'id',
        type:
          dbType === 'postgresql'
            ? 'uuid'
            : dbType === 'oracle'
              ? 'number'
              : 'int',
        nullable: false,
        isPrimaryKey: true,
        isForeignKey: false,
        defaultValue: dbType === 'postgresql' ? 'gen_random_uuid()' : null,
      },
      {
        name: 'user_id',
        type:
          dbType === 'postgresql'
            ? 'uuid'
            : dbType === 'oracle'
              ? 'number'
              : 'int',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: true,
        referencedTable: 'users',
        referencedColumn: 'id',
      },
      {
        name: 'status',
        type:
          dbType === 'postgresql'
            ? 'text'
            : dbType === 'mysql'
              ? 'enum'
              : 'varchar(50)',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
        defaultValue: dbType === 'mysql' ? "'pending'" : "'pending'",
      },
      {
        name: 'total_amount',
        type:
          dbType === 'postgresql'
            ? 'numeric(10,2)'
            : dbType === 'mysql'
              ? 'decimal(10,2)'
              : 'decimal(10,2)',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
      },
      {
        name: 'created_at',
        type:
          dbType === 'postgresql'
            ? 'timestamp with time zone'
            : dbType === 'mysql'
              ? 'datetime'
              : 'timestamp',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
        defaultValue: dbType === 'postgresql' ? 'now()' : 'CURRENT_TIMESTAMP',
      },
    ],
    primaryKeys: ['id'],
    foreignKeys: [
      {
        column: 'user_id',
        referencedTable: 'users',
        referencedColumn: 'id',
      },
    ],
  }
  tables.push(ordersTable)

  // Order items table
  const orderItemsTable: TableInfo = {
    name: 'order_items',
    schema: schemas[0],
    columns: [
      {
        name: 'id',
        type:
          dbType === 'postgresql'
            ? 'uuid'
            : dbType === 'oracle'
              ? 'number'
              : 'int',
        nullable: false,
        isPrimaryKey: true,
        isForeignKey: false,
        defaultValue: dbType === 'postgresql' ? 'gen_random_uuid()' : null,
      },
      {
        name: 'order_id',
        type:
          dbType === 'postgresql'
            ? 'uuid'
            : dbType === 'oracle'
              ? 'number'
              : 'int',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: true,
        referencedTable: 'orders',
        referencedColumn: 'id',
      },
      {
        name: 'product_id',
        type:
          dbType === 'postgresql'
            ? 'uuid'
            : dbType === 'oracle'
              ? 'number'
              : 'int',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: true,
        referencedTable: 'products',
        referencedColumn: 'id',
      },
      {
        name: 'quantity',
        type:
          dbType === 'postgresql'
            ? 'integer'
            : dbType === 'oracle'
              ? 'number'
              : 'int',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
        defaultValue: '1',
      },
      {
        name: 'unit_price',
        type:
          dbType === 'postgresql'
            ? 'numeric(10,2)'
            : dbType === 'mysql'
              ? 'decimal(10,2)'
              : 'decimal(10,2)',
        nullable: false,
        isPrimaryKey: false,
        isForeignKey: false,
      },
    ],
    primaryKeys: ['id'],
    foreignKeys: [
      {
        column: 'order_id',
        referencedTable: 'orders',
        referencedColumn: 'id',
      },
      {
        column: 'product_id',
        referencedTable: 'products',
        referencedColumn: 'id',
      },
    ],
  }
  tables.push(orderItemsTable)

  // Add a couple more specific tables based on the DB type
  if (dbType === 'postgresql') {
    // Add a Postgres-specific table with JSON data
    const analyticsEventsTable: TableInfo = {
      name: 'analytics_events',
      schema: 'analytics',
      columns: [
        {
          name: 'id',
          type: 'uuid',
          nullable: false,
          isPrimaryKey: true,
          isForeignKey: false,
          defaultValue: 'gen_random_uuid()',
        },
        {
          name: 'event_name',
          type: 'text',
          nullable: false,
          isPrimaryKey: false,
          isForeignKey: false,
        },
        {
          name: 'user_id',
          type: 'uuid',
          nullable: true,
          isPrimaryKey: false,
          isForeignKey: true,
          referencedTable: 'users',
          referencedColumn: 'id',
        },
        {
          name: 'payload',
          type: 'jsonb',
          nullable: true,
          isPrimaryKey: false,
          isForeignKey: false,
        },
        {
          name: 'created_at',
          type: 'timestamp with time zone',
          nullable: false,
          isPrimaryKey: false,
          isForeignKey: false,
          defaultValue: 'now()',
        },
      ],
      primaryKeys: ['id'],
      foreignKeys: [
        {
          column: 'user_id',
          referencedTable: 'users',
          referencedColumn: 'id',
        },
      ],
    }
    tables.push(analyticsEventsTable)
  } else if (dbType === 'sqlserver') {
    // Add a SQL Server-specific table with temporal data
    const employeeHistoryTable: TableInfo = {
      name: 'employee_history',
      schema: 'hr',
      columns: [
        {
          name: 'id',
          type: 'int',
          nullable: false,
          isPrimaryKey: true,
          isForeignKey: false,
        },
        {
          name: 'employee_id',
          type: 'int',
          nullable: false,
          isPrimaryKey: false,
          isForeignKey: true,
          referencedTable: 'employees',
          referencedColumn: 'id',
        },
        {
          name: 'department',
          type: 'nvarchar(100)',
          nullable: false,
          isPrimaryKey: false,
          isForeignKey: false,
        },
        {
          name: 'salary',
          type: 'decimal(12,2)',
          nullable: false,
          isPrimaryKey: false,
          isForeignKey: false,
        },
        {
          name: 'valid_from',
          type: 'datetime2',
          nullable: false,
          isPrimaryKey: false,
          isForeignKey: false,
        },
        {
          name: 'valid_to',
          type: 'datetime2',
          nullable: false,
          isPrimaryKey: false,
          isForeignKey: false,
        },
      ],
      primaryKeys: ['id'],
      foreignKeys: [
        {
          column: 'employee_id',
          referencedTable: 'employees',
          referencedColumn: 'id',
        },
      ],
    }
    tables.push(employeeHistoryTable)

    // Add employees table that is referenced by history
    const employeesTable: TableInfo = {
      name: 'employees',
      schema: 'hr',
      columns: [
        {
          name: 'id',
          type: 'int',
          nullable: false,
          isPrimaryKey: true,
          isForeignKey: false,
          defaultValue: null,
        },
        {
          name: 'first_name',
          type: 'nvarchar(50)',
          nullable: false,
          isPrimaryKey: false,
          isForeignKey: false,
        },
        {
          name: 'last_name',
          type: 'nvarchar(50)',
          nullable: false,
          isPrimaryKey: false,
          isForeignKey: false,
        },
        {
          name: 'email',
          type: 'nvarchar(255)',
          nullable: false,
          isPrimaryKey: false,
          isForeignKey: false,
        },
        {
          name: 'hire_date',
          type: 'date',
          nullable: false,
          isPrimaryKey: false,
          isForeignKey: false,
        },
      ],
      primaryKeys: ['id'],
      foreignKeys: [],
    }
    tables.push(employeesTable)
  }

  return tables
}

export function generateMockMetadata(dbType: DatabaseType): DatabaseMetadata {
  const tables = generateTables(dbType)
  const schemas = [...new Set(tables.map((table) => table.schema))]

  return {
    name: `sample_${dbType}_db`,
    type: dbType,
    tables,
    schemas,
  }
}
