import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";

const API_BASE = process.env.REACT_APP_API_BASE_URL || "https://localhost:56909";

export interface PricingRecord {
  id: string;
  storeId: string;
  sku: string;
  productName: string;
  price: number;
  currencyCode: string;
  effectiveDate: string;
  createdAt: string;
  lastModifiedAt?: string;
  lastModifiedBy?: string;
}

export interface SearchFilters {
  storeId: string;
  sku: string;
  productName: string;
  minPrice: string;
  maxPrice: string;
  dateFrom: string;
  dateTo: string;
}

export interface SearchResult {
  items: PricingRecord[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

interface SearchState {
  filters: SearchFilters;
  results: SearchResult | null;
  loading: boolean;
  error: string | null;
  pageNumber: number;
  pageSize: number;
  editingRecord: PricingRecord | null;
  saving: boolean;
  saveError: string | null;
}

const initialFilters: SearchFilters = {
  storeId: "",
  sku: "",
  productName: "",
  minPrice: "",
  maxPrice: "",
  dateFrom: "",
  dateTo: "",
};

const initialState: SearchState = {
  filters: initialFilters,
  results: null,
  loading: false,
  error: null,
  pageNumber: 1,
  pageSize: 50,
  editingRecord: null,
  saving: false,
  saveError: null,
};

export const searchPricingRecords = createAsyncThunk(
  "search/searchPricingRecords",
  async (
    { filters, pageNumber, pageSize, token }: {
      filters: SearchFilters; pageNumber: number; pageSize: number; token: string;
    },
    { rejectWithValue }
  ) => {
    const params = new URLSearchParams();
    if (filters.storeId) params.set("storeId", filters.storeId);
    if (filters.sku) params.set("sku", filters.sku);
    if (filters.productName) params.set("productName", filters.productName);
    if (filters.minPrice) params.set("minPrice", filters.minPrice);
    if (filters.maxPrice) params.set("maxPrice", filters.maxPrice);
    if (filters.dateFrom) params.set("dateFrom", filters.dateFrom);
    if (filters.dateTo) params.set("dateTo", filters.dateTo);
    params.set("pageNumber", String(pageNumber));
    params.set("pageSize", String(pageSize));

    const response = await fetch(
      `${API_BASE}/api/v1/pricing/search?${params.toString()}`,
      { headers: { Authorization: `Bearer ${token}` } }
    );

    if (!response.ok) return rejectWithValue("Search failed. Please try again.");
    return (await response.json()) as SearchResult;
  }
);

export const updatePricingRecord = createAsyncThunk(
  "search/updatePricingRecord",
  async (
    { id, price, productName, token }: {
      id: string; price: number; productName: string; token: string;
    },
    { rejectWithValue }
  ) => {
    const response = await fetch(`${API_BASE}/api/v1/pricing/${id}`, {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token}`,
      },
      body: JSON.stringify({ price, productName }),
    });

    if (!response.ok) {
      const body = await response.json().catch(() => ({}));
      return rejectWithValue(body?.message || "Update failed.");
    }

    return (await response.json()) as PricingRecord;
  }
);

const searchSlice = createSlice({
  name: "search",
  initialState,
  reducers: {
    setFilters(state, action: PayloadAction<Partial<SearchFilters>>) {
      state.filters = { ...state.filters, ...action.payload };
      state.pageNumber = 1; // reset to first page on filter change
    },
    clearFilters(state) {
      state.filters = initialFilters;
      state.results = null;
      state.pageNumber = 1;
    },
    setPage(state, action: PayloadAction<number>) {
      state.pageNumber = action.payload;
    },
    openEditRecord(state, action: PayloadAction<PricingRecord>) {
      state.editingRecord = action.payload;
      state.saveError = null;
    },
    closeEditRecord(state) {
      state.editingRecord = null;
      state.saveError = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(searchPricingRecords.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(searchPricingRecords.fulfilled, (state, action) => {
        state.loading = false;
        state.results = action.payload;
      })
      .addCase(searchPricingRecords.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      })
      .addCase(updatePricingRecord.pending, (state) => {
        state.saving = true;
        state.saveError = null;
      })
      .addCase(updatePricingRecord.fulfilled, (state, action) => {
        state.saving = false;
        state.editingRecord = null;
        // Update the record in the results list in-place
        if (state.results) {
          const idx = state.results.items.findIndex((r) => r.id === action.payload.id);
          if (idx !== -1) {
            state.results.items[idx] = action.payload;
          }
        }
      })
      .addCase(updatePricingRecord.rejected, (state, action) => {
        state.saving = false;
        state.saveError = action.payload as string;
      });
  },
});

export const {
  setFilters, clearFilters, setPage, openEditRecord, closeEditRecord,
} = searchSlice.actions;
export default searchSlice.reducer;
