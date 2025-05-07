import { UseQueryResult } from '@tanstack/react-query'

export interface ODataParams {
  pageIndex: number
  pageSize: number
  sorting: Array<{ id: string; desc: boolean }>
  columnFilters: Array<{ id: string; value: string }>
}

export interface ODataResponse<T> {
  value: T[]
  '@odata.count'?: number | null
}

export type ODataQueryResult<T> = UseQueryResult<ODataResponse<T>>
