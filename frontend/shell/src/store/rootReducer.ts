import { combineReducers } from "@reduxjs/toolkit";

// Async function to load reducers from remote MFEs
export async function createRootReducer() {
  const searchReducerModule = await import("mfeSearch/searchSlice");
  const uploadReducerModule = await import("mfeUpload/uploadSlice");

  return combineReducers({
    search: searchReducerModule.default,
    upload: uploadReducerModule.default,
  });
}

export type RootState = ReturnType<Awaited<ReturnType<typeof createRootReducer>>>;
