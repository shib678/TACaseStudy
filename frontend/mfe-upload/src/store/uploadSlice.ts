import { createSlice, createAsyncThunk, PayloadAction } from "@reduxjs/toolkit";

const API_BASE = process.env.REACT_APP_API_BASE_URL || "https://localhost:56909";

export interface RowError {
  rowNumber: number;
  errors: string[];
}

export interface UploadResult {
  batchId: string;
  totalRows: number;
  processedRows: number;
  failedRows: number;
  rowErrors: RowError[];
  status: string;
}

export interface UploadBatchSummary {
  id: string;
  fileName: string;
  storeId: string;
  totalRows: number;
  processedRows: number;
  failedRows: number;
  status: string;
  errorSummary?: string;
  createdAt: string;
  createdBy: string;
}

interface UploadState {
  uploading: boolean;
  uploadResult: UploadResult | null;
  history: UploadBatchSummary[];
  historyLoading: boolean;
  error: string | null;
  clientValidationErrors: string[];
}

const initialState: UploadState = {
  uploading: false,
  uploadResult: null,
  history: [],
  historyLoading: false,
  error: null,
  clientValidationErrors: [],
};

export const uploadPricingFeed = createAsyncThunk(
  "upload/uploadPricingFeed",
  async (
    { file, storeId, token }: { file: File; storeId: string; token: string },
    { rejectWithValue }
  ) => {
    const formData = new FormData();
    formData.append("file", file);

    const response = await fetch(
      `${API_BASE}/api/v1/upload?storeId=${encodeURIComponent(storeId)}`,
      {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
        body: formData,
      }
    );

    if (!response.ok) {
      const errorBody = await response.json().catch(() => ({}));
      return rejectWithValue(errorBody?.errors?.join(", ") || "Upload failed.");
    }

    return (await response.json()) as UploadResult;
  }
);

export const fetchUploadHistory = createAsyncThunk(
  "upload/fetchHistory",
  async (
    { storeId, token }: { storeId: string; token: string },
    { rejectWithValue }
  ) => {
    const response = await fetch(
      `${API_BASE}/api/v1/upload/history/${encodeURIComponent(storeId)}`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    if (!response.ok) return rejectWithValue("Failed to load upload history.");
    return (await response.json()) as UploadBatchSummary[];
  }
);

const uploadSlice = createSlice({
  name: "upload",
  initialState,
  reducers: {
    clearUploadResult(state) {
      state.uploadResult = null;
      state.error = null;
      state.clientValidationErrors = [];
    },
    setClientValidationErrors(state, action: PayloadAction<string[]>) {
      state.clientValidationErrors = action.payload;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(uploadPricingFeed.pending, (state) => {
        state.uploading = true;
        state.error = null;
        state.uploadResult = null;
      })
      .addCase(uploadPricingFeed.fulfilled, (state, action) => {
        state.uploading = false;
        state.uploadResult = action.payload;
      })
      .addCase(uploadPricingFeed.rejected, (state, action) => {
        state.uploading = false;
        state.error = action.payload as string;
      })
      .addCase(fetchUploadHistory.pending, (state) => {
        state.historyLoading = true;
      })
      .addCase(fetchUploadHistory.fulfilled, (state, action) => {
        state.historyLoading = false;
        state.history = action.payload;
      })
      .addCase(fetchUploadHistory.rejected, (state) => {
        state.historyLoading = false;
      });
  },
});

export const { clearUploadResult, setClientValidationErrors } = uploadSlice.actions;
export default uploadSlice.reducer;
