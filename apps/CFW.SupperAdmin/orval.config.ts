// orval.config.ts
export default {
    api: {
      input: 'http://127.0.0.1:5000/openapi/v1.json',
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
  