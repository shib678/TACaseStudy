import React from "react";
import ReactDOM from "react-dom/client";
import { Provider } from "react-redux";
import { configureStore } from "@reduxjs/toolkit";
import App from "./App";
import searchReducer from "./store/searchSlice";

const store = configureStore({ reducer: { search: searchReducer } });

const root = ReactDOM.createRoot(document.getElementById("root") as HTMLElement);
root.render(
  <Provider store={store}>
    <App standalone />
  </Provider>
);
