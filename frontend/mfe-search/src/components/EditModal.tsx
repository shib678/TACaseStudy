import React, { useState, useEffect } from "react";
import {
  Alert, Box, Button, CircularProgress, Dialog, DialogActions,
  DialogContent, DialogTitle, InputAdornment, TextField, Typography,
} from "@mui/material";
import { useDispatch, useSelector } from "react-redux";
import { closeEditRecord, updatePricingRecord } from "../store/searchSlice";

interface EditModalProps {
  authToken?: string;
}

const EditModal: React.FC<EditModalProps> = ({ authToken = "" }) => {
  const dispatch = useDispatch<any>();
  const { editingRecord, saving, saveError } = useSelector((s: any) => s.search);

  const [price, setPrice] = useState("");
  const [productName, setProductName] = useState("");
  const [priceError, setPriceError] = useState("");

  useEffect(() => {
    if (editingRecord) {
      setPrice(String(editingRecord.price));
      setProductName(editingRecord.productName);
      setPriceError("");
    }
  }, [editingRecord]);

  if (!editingRecord) return null;

  const validate = () => {
    const p = parseFloat(price);
    if (isNaN(p) || p <= 0) {
      setPriceError("Price must be greater than 0.");
      return false;
    }
    if (!/^\d+(\.\d{1,2})?$/.test(price)) {
      setPriceError("Price must have at most 2 decimal places.");
      return false;
    }
    setPriceError("");
    return true;
  };

  const handleSave = () => {
    if (!validate()) return;
    dispatch(
      updatePricingRecord({
        id: editingRecord.id,
        price: parseFloat(price),
        productName,
        token: authToken,
      })
    );
  };

  const handleClose = () => dispatch(closeEditRecord());

  return (
    <Dialog open={!!editingRecord} onClose={handleClose} maxWidth="sm" fullWidth>
      <DialogTitle>Edit Pricing Record</DialogTitle>
      <DialogContent>
        {saveError && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {saveError}
          </Alert>
        )}

        <Box display="flex" gap={1} mb={2} mt={1}>
          <Typography variant="caption" color="text.secondary">
            <strong>Store:</strong> {editingRecord.storeId}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            &bull; <strong>SKU:</strong> {editingRecord.sku}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            &bull; <strong>Date:</strong> {editingRecord.effectiveDate}
          </Typography>
        </Box>

        <TextField
          label="Product Name"
          value={productName}
          onChange={(e) => setProductName(e.target.value)}
          fullWidth
          required
          inputProps={{ maxLength: 200 }}
          sx={{ mb: 2 }}
        />

        <TextField
          label="Price"
          value={price}
          onChange={(e) => setPrice(e.target.value)}
          onBlur={validate}
          fullWidth
          required
          type="number"
          inputProps={{ min: 0.01, step: "0.01" }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                {editingRecord.currencyCode}
              </InputAdornment>
            ),
          }}
          error={!!priceError}
          helperText={priceError}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose} disabled={saving}>
          Cancel
        </Button>
        <Button
          variant="contained"
          onClick={handleSave}
          disabled={saving || !productName.trim()}
          startIcon={saving ? <CircularProgress size={16} /> : null}
        >
          {saving ? "Saving..." : "Save Changes"}
        </Button>
      </DialogActions>
    </Dialog>
  );
};

export default EditModal;
