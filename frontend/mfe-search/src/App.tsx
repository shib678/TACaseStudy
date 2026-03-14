import React from "react";
import { Box, Typography } from "@mui/material";
import { configureStore } from "@reduxjs/toolkit";
import { Provider } from "react-redux";
import SearchForm from "./components/SearchForm";
import PricingGrid from "./components/PricingGrid";
import searchReducer from "./store/searchSlice";

const standaloneStore = configureStore({
  reducer: { search: searchReducer },
});

interface AppProps {
  standalone?: boolean;
  authToken?: string;
}

const SearchPage: React.FC<{ authToken?: string }> = ({ authToken = "" }) => (
  <Box>
    <Typography variant="h5" gutterBottom fontWeight={600}>
      Pricing Records
    </Typography>
    <SearchForm authToken={authToken} />
    <PricingGrid authToken={authToken} />
  </Box>
);

const App: React.FC<AppProps> = ({ standalone = false, authToken }) => {
  if (standalone) {
    return (
      <Provider store={standaloneStore}>
        <SearchPage authToken={authToken} />
      </Provider>
    );
  }
  return <SearchPage authToken={authToken} />;
};

export default App;
