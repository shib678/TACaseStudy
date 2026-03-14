import React from "react";
import { Box } from "@mui/material";
import UploadForm from "./components/UploadForm";
import UploadHistory from "./components/UploadHistory";
import { configureStore } from "@reduxjs/toolkit";
import { Provider } from "react-redux";
import uploadReducer from "./store/uploadSlice";

// When loaded as MFE in shell, the shell injects the store.
// When run standalone, we create a local store.
const standaloneStore = configureStore({
  reducer: { upload: uploadReducer },
});

interface AppProps {
  standalone?: boolean;
  authToken?: string;
  storeId?: string;
}

const UploadPage: React.FC<{ authToken?: string; storeId?: string }> = ({
  authToken = "",
  storeId = "",
}) => (
  <Box>
    <UploadForm authToken={authToken} />
    <UploadHistory storeId={storeId} authToken={authToken} />
  </Box>
);

const App: React.FC<AppProps> = ({ standalone = false, authToken, storeId }) => {
  if (standalone) {
    return (
      <Provider store={standaloneStore}>
        <UploadPage authToken={authToken} storeId={storeId} />
      </Provider>
    );
  }
  return <UploadPage authToken={authToken} storeId={storeId} />;
};

export default App;
