import { configureStore } from "@reduxjs/toolkit";
import { persistStore, persistReducer, FLUSH, REHYDRATE, PAUSE, PERSIST, PURGE, REGISTER } from "redux-persist";
import storage from "redux-persist/lib/storage";
import { createRootReducer } from "./rootReducer";

const persistConfig = {
  key: "retail-pricing-root",
  version: 1,
  storage,
  whitelist: ["search"], // Only persist search preferences, not upload state
};

export async function createStore() {
  const rootReducer = await createRootReducer();
  const persistedReducer = persistReducer(persistConfig, rootReducer);

  const store = configureStore({
    reducer: persistedReducer,
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware({
        serializableCheck: {
          ignoredActions: [FLUSH, REHYDRATE, PAUSE, PERSIST, PURGE, REGISTER],
        },
      }),
  });

  const persistor = persistStore(store);

  return { store, persistor };
}

export type RootState = Awaited<ReturnType<typeof createRootReducer>> extends infer R
  ? R extends (...args: any) => infer S
    ? S
    : never
  : never;
export type AppDispatch = Awaited<ReturnType<typeof createStore>> extends { store: infer S }
  ? S extends { dispatch: infer D }
    ? D
    : never
  : never;
