import { create } from 'zustand'

export type CommonStateKey = string

interface CommonEntityState<T = any> {
  open: string | null
  setOpen: (value: string | null) => void
  currentRow: T | null
  setCurrentRow: (value: T | null) => void
}

type InternalStore = Record<CommonStateKey, CommonEntityState<any>>

interface CommonStateStore {
  stores: InternalStore
  setOpen: (key: CommonStateKey, value: string | null) => void
  setCurrentRow: <T>(key: CommonStateKey, row: T | null) => void
  getState: <T = any>(key: CommonStateKey) => CommonEntityState<T>
}

export const useCommonState = create<CommonStateStore>((set, get) => ({
  stores: {},

  setOpen: (key, value) =>
    set((state) => ({
      stores: {
        ...state.stores,
        [key]: {
          ...state.stores[key],
          open: value,
        },
      },
    })),

  setCurrentRow: (key, row) =>
    set((state) => ({
      stores: {
        ...state.stores,
        [key]: {
          ...state.stores[key],
          currentRow: row,
        },
      },
    })),

  getState: (key) => {
    const stores = get().stores
    if (!stores[key]) {
      const initial: CommonEntityState = {
        open: null,
        setOpen: (val) => get().setOpen(key, val),
        currentRow: null,
        setCurrentRow: (val) => get().setCurrentRow(key, val),
      }
      set((state) => ({
        stores: {
          ...state.stores,
          [key]: initial,
        },
      }))
      return initial
    }
    return {
      ...stores[key],
      setOpen: (val) => get().setOpen(key, val),
      setCurrentRow: (val) => get().setCurrentRow(key, val),
    }
  },
}))
