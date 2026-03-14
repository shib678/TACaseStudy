import React, { Suspense, lazy } from "react";
import { Routes, Route, Navigate } from "react-router-dom";
import { Box, CircularProgress, Typography } from "@mui/material";
import Layout from "./components/Layout";

// Lazy-load microfrontends via Module Federation
const UploadApp = lazy(() => import("mfeUpload/App"));
const SearchApp = lazy(() => import("mfeSearch/App"));

const MfeLoader = () => (
  <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
    <CircularProgress />
  </Box>
);

const App: React.FC = () => {
  return (
    <Layout>
      <Suspense fallback={<MfeLoader />}>
        <Routes>
          <Route path="/" element={<Navigate to="/search" replace />} />
          <Route path="/upload/*" element={<UploadApp />} />
          <Route path="/search/*" element={<SearchApp />} />
          <Route
            path="*"
            element={
              <Box p={4} textAlign="center">
                <Typography variant="h5">Page not found</Typography>
              </Box>
            }
          />
        </Routes>
      </Suspense>
    </Layout>
  );
};

export default App;
