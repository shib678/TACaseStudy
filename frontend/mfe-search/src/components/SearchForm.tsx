import React, { useCallback } from "react";
import {
  Box, Button, Collapse, Grid, Paper, TextField, Typography,
} from "@mui/material";
import SearchIcon from "@mui/icons-material/Search";
import ClearIcon from "@mui/icons-material/Clear";
import { useDispatch, useSelector } from "react-redux";
import { searchPricingRecords, setFilters, clearFilters, SearchFilters } from "../store/searchSlice";

interface SearchFormProps {
  authToken?: string;
}

const SearchForm: React.FC<SearchFormProps> = ({ authToken = "" }) => {
  const dispatch = useDispatch<any>();
  const { filters, pageNumber, pageSize, loading } = useSelector((s: any) => s.search);

  const handleChange = useCallback(
    (field: keyof SearchFilters) => (e: React.ChangeEvent<HTMLInputElement>) => {
      dispatch(setFilters({ [field]: e.target.value }));
    },
    [dispatch]
  );

  const handleSearch = useCallback(() => {
    dispatch(searchPricingRecords({ filters, pageNumber, pageSize, token: authToken }));
  }, [dispatch, filters, pageNumber, pageSize, authToken]);

  const handleClear = useCallback(() => {
    dispatch(clearFilters());
  }, [dispatch]);

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") handleSearch();
  };

  return (
    <Paper elevation={2} sx={{ p: 3, mb: 3 }}>
      <Typography variant="h6" gutterBottom fontWeight={600}>
        Search Pricing Records
      </Typography>

      <Grid container spacing={2} onKeyDown={handleKeyDown}>
        <Grid item xs={12} sm={6} md={3}>
          <TextField
            label="Store ID"
            value={filters.storeId}
            onChange={handleChange("storeId")}
            fullWidth
            placeholder="e.g. AU-1001"
            size="small"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <TextField
            label="SKU"
            value={filters.sku}
            onChange={handleChange("sku")}
            fullWidth
            placeholder="e.g. SKU-ABC123"
            size="small"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <TextField
            label="Product Name"
            value={filters.productName}
            onChange={handleChange("productName")}
            fullWidth
            placeholder="Partial match supported"
            size="small"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <TextField
            label="Date From"
            type="date"
            value={filters.dateFrom}
            onChange={handleChange("dateFrom")}
            fullWidth
            InputLabelProps={{ shrink: true }}
            size="small"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <TextField
            label="Date To"
            type="date"
            value={filters.dateTo}
            onChange={handleChange("dateTo")}
            fullWidth
            InputLabelProps={{ shrink: true }}
            size="small"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={2}>
          <TextField
            label="Min Price"
            type="number"
            value={filters.minPrice}
            onChange={handleChange("minPrice")}
            fullWidth
            inputProps={{ min: 0, step: "0.01" }}
            size="small"
          />
        </Grid>
        <Grid item xs={12} sm={6} md={2}>
          <TextField
            label="Max Price"
            type="number"
            value={filters.maxPrice}
            onChange={handleChange("maxPrice")}
            fullWidth
            inputProps={{ min: 0, step: "0.01" }}
            size="small"
          />
        </Grid>

        <Grid item xs={12}>
          <Box display="flex" gap={2}>
            <Button
              variant="contained"
              startIcon={<SearchIcon />}
              onClick={handleSearch}
              disabled={loading}
            >
              Search
            </Button>
            <Button
              variant="outlined"
              startIcon={<ClearIcon />}
              onClick={handleClear}
              disabled={loading}
            >
              Clear
            </Button>
          </Box>
        </Grid>
      </Grid>
    </Paper>
  );
};

export default SearchForm;
