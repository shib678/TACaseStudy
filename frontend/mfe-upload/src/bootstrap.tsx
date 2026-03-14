import React from "react";
import ReactDOM from "react-dom/client";
import { Provider } from "react-redux";
import { configureStore } from "@reduxjs/toolkit";
import App from "./App";
import uploadReducer from "./store/uploadSlice";

// Standalone entry for local development
const store = configureStore({ reducer: { upload: uploadReducer } });

const root = ReactDOM.createRoot(document.getElementById("root") as HTMLElement);
root.render(
  <Provider store={store}>
    <App standalone />
  </Provider>
);
