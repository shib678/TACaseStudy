import React, { useCallback, useState } from "react";
import { useDropzone } from "react-dropzone";
import Papa from "papaparse";
import {
  Alert, Box, Button, Chip, CircularProgress, Divider,
  LinearProgress, Paper, TextField, Typography,
} from "@mui/material";
import UploadFileIcon from "@mui/icons-material/UploadFile";
import CheckCircleIcon from "@mui/icons-material/CheckCircle";
import ErrorIcon from "@mui/icons-material/Error";
import { useDispatch, useSelector } from "react-redux";
import {
  clearUploadResult,
  setClientValidationErrors,
  uploadPricingFeed,
} from "../store/uploadSlice";

const REQUIRED_HEADERS = ["StoreId", "SKU", "ProductName", "Price", "Date"];
const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10 MB

interface UploadFormProps {
  authToken?: string;
}

const UploadForm: React.FC<UploadFormProps> = ({ authToken = "" }) => {
  const dispatch = useDispatch<any>();
  const { uploading, uploadResult, error, clientValidationErrors } = useSelector(
    (state: any) => state.upload
  );

  const [storeId, setStoreId] = useState("");
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  const validateCsvClient = useCallback(
    (file: File): Promise<string[]> => {
      return new Promise((resolve) => {
        Papa.parse(file, {
          header: true,
          preview: 2,
          complete: (results) => {
            const errors: string[] = [];
            const headers = results.meta.fields || [];

            const missing = REQUIRED_HEADERS.filter(
              (h) => !headers.some((fh) => fh.toLowerCase() === h.toLowerCase())
            );
            if (missing.length > 0)
              errors.push(`Missing required columns: ${missing.join(", ")}`);

            if (file.size > MAX_FILE_SIZE)
              errors.push("File exceeds maximum size of 10 MB.");

            resolve(errors);
          },
          error: () => resolve(["Unable to parse CSV file."]),
        });
      });
    },
    []
  );

  const onDrop = useCallback(
    async (acceptedFiles: File[]) => {
      const file = acceptedFiles[0];
      if (!file) return;

      dispatch(clearUploadResult());
      const errs = await validateCsvClient(file);
      dispatch(setClientValidationErrors(errs));

      if (errs.length === 0) setSelectedFile(file);
      else setSelectedFile(null);
    },
    [dispatch, validateCsvClient]
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { "text/csv": [".csv"] },
    maxFiles: 1,
  });

  const handleUpload = () => {
    if (!selectedFile || !storeId.trim()) return;
    dispatch(uploadPricingFeed({ file: selectedFile, storeId: storeId.trim(), token: authToken }));
  };

  const handleReset = () => {
    setSelectedFile(null);
    setStoreId("");
    dispatch(clearUploadResult());
  };

  return (
    <Box>
      <Typography variant="h5" gutterBottom fontWeight={600}>
        Upload Pricing Feed
      </Typography>
      <Typography variant="body2" color="text.secondary" mb={3}>
        Upload a CSV file containing pricing data for your store. Supported format: StoreId, SKU,
        ProductName, Price, Date (yyyy-MM-dd).
      </Typography>

      <Paper elevation={2} sx={{ p: 3, mb: 3 }}>
        {/* Store ID */}
        <TextField
          label="Store ID"
          value={storeId}
          onChange={(e) => setStoreId(e.target.value)}
          fullWidth
          required
          placeholder="e.g. AU-1001"
          helperText="Enter the store ID this pricing feed belongs to"
          sx={{ mb: 3 }}
          disabled={uploading}
        />

        {/* Dropzone */}
        <Box
          {...getRootProps()}
          sx={{
            border: "2px dashed",
            borderColor: isDragActive ? "primary.main" : "divider",
            borderRadius: 2,
            p: 4,
            textAlign: "center",
            cursor: "pointer",
            backgroundColor: isDragActive ? "action.hover" : "background.paper",
            transition: "all 0.2s",
            mb: 2,
          }}
        >
          <input {...getInputProps()} />
          <UploadFileIcon sx={{ fontSize: 48, color: "text.secondary", mb: 1 }} />
          {isDragActive ? (
            <Typography>Drop the CSV file here...</Typography>
          ) : (
            <>
              <Typography>Drag & drop a CSV file, or click to select</Typography>
              <Typography variant="caption" color="text.secondary">
                Maximum file size: 10 MB
              </Typography>
            </>
          )}
        </Box>

        {/* Selected File Chip */}
        {selectedFile && (
          <Box mb={2}>
            <Chip
              icon={<CheckCircleIcon />}
              label={`${selectedFile.name} (${(selectedFile.size / 1024).toFixed(1)} KB)`}
              color="success"
              variant="outlined"
            />
          </Box>
        )}

        {/* Client-side validation errors */}
        {clientValidationErrors.length > 0 && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {clientValidationErrors.map((e, i) => (
              <div key={i}>{e}</div>
            ))}
          </Alert>
        )}

        {/* Action Buttons */}
        <Box display="flex" gap={2}>
          <Button
            variant="contained"
            onClick={handleUpload}
            disabled={!selectedFile || !storeId.trim() || uploading}
            startIcon={uploading ? <CircularProgress size={18} /> : <UploadFileIcon />}
          >
            {uploading ? "Uploading..." : "Upload"}
          </Button>
          <Button variant="outlined" onClick={handleReset} disabled={uploading}>
            Reset
          </Button>
        </Box>

        {uploading && <LinearProgress sx={{ mt: 2 }} />}
      </Paper>

      {/* Upload Error */}
      {error && (
        <Alert severity="error" sx={{ mb: 2 }} icon={<ErrorIcon />}>
          Upload failed: {error}
        </Alert>
      )}

      {/* Upload Result */}
      {uploadResult && (
        <Paper elevation={2} sx={{ p: 3 }}>
          <Typography variant="h6" gutterBottom>
            Upload Result
          </Typography>
          <Divider sx={{ mb: 2 }} />

          <Box display="flex" gap={2} flexWrap="wrap" mb={2}>
            <Chip label={`Batch ID: ${uploadResult.batchId}`} variant="outlined" />
            <Chip
              label={`Status: ${uploadResult.status}`}
              color={uploadResult.failedRows === 0 ? "success" : "warning"}
            />
            <Chip label={`Total: ${uploadResult.totalRows}`} />
            <Chip label={`Processed: ${uploadResult.processedRows}`} color="success" />
            {uploadResult.failedRows > 0 && (
              <Chip label={`Failed: ${uploadResult.failedRows}`} color="error" />
            )}
          </Box>

          {uploadResult.rowErrors.length > 0 && (
            <Box>
              <Typography variant="subtitle2" color="error" gutterBottom>
                Row Errors ({uploadResult.rowErrors.length}):
              </Typography>
              <Box
                component="ul"
                sx={{ maxHeight: 200, overflowY: "auto", pl: 2, m: 0 }}
              >
                {uploadResult.rowErrors.map((re) => (
                  <li key={re.rowNumber}>
                    <Typography variant="caption">
                      <strong>Row {re.rowNumber}:</strong> {re.errors.join("; ")}
                    </Typography>
                  </li>
                ))}
              </Box>
            </Box>
          )}
        </Paper>
      )}
    </Box>
  );
};

export default UploadForm;
