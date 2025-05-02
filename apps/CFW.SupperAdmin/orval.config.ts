// orval.config.ts
export default {
    api: {
      input: 'http://localhost:5000/openapi/v1.json',
      output: {
        target: './src/api',
        client: 'react-query',
        mode: 'tags-split',
        schemas: './src/api/model',
        override: {
          mutator: {
            path: './src/lib/axios.ts',
            name: 'customAxiosFunction',
          },
        },
      },
    },
  }
  