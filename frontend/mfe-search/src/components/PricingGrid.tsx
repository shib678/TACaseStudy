import React, { useCallback } from "react";
import {
  Alert, Box, Chip, CircularProgress, IconButton, Pagination,
  Paper, Table, TableBody, TableCell, TableContainer, TableHead,
  TableRow, Tooltip, Typography,
} from "@mui/material";
import EditIcon from "@mui/icons-material/Edit";
import { useDispatch, useSelector } from "react-redux";
import {
  openEditRecord,
  searchPricingRecords,
  setPage,
  PricingRecord,
} from "../store/searchSlice";
import EditModal from "./EditModal";

interface PricingGridProps {
  authToken?: string;
}

const currencyFormatter = (price: number, currency: string) => {
  try {
    return new Intl.NumberFormat("en", {
      style: "currency",
      currency,
      minimumFractionDigits: 2,
    }).format(price);
  } catch {
    return `${currency} ${price.toFixed(2)}`;
  }
};

const PricingGrid: React.FC<PricingGridProps> = ({ authToken = "" }) => {
  const dispatch = useDispatch<any>();
  const { results, loading, error, filters, pageNumber, pageSize } = useSelector(
    (s: any) => s.search
  );

  const handlePageChange = useCallback(
    (_: unknown, page: number) => {
      dispatch(setPage(page));
      dispatch(searchPricingRecords({ filters, pageNumber: page, pageSize, token: authToken }));
    },
    [dispatch, filters, pageSize, authToken]
  );

  const handleEdit = useCallback(
    (record: PricingRecord) => {
      dispatch(openEditRecord(record));
    },
    [dispatch]
  );

  if (!results && !loading) {
    return (
      <Box textAlign="center" py={6} color="text.secondary">
        <Typography>Use the search filters above to find pricing records.</Typography>
      </Box>
    );
  }

  if (loading) {
    return (
      <Box display="flex" justifyContent="center" py={6}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return <Alert severity="error">{error}</Alert>;
  }

  return (
    <Box>
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={1}>
        <Typography variant="body2" color="text.secondary">
          {results!.totalCount} records found
        </Typography>
        {results!.totalPages > 1 && (
          <Pagination
            count={results!.totalPages}
            page={results!.pageNumber}
            onChange={handlePageChange}
            color="primary"
            size="small"
          />
        )}
      </Box>

      <TableContainer component={Paper} elevation={2}>
        <Table size="small" stickyHeader>
          <TableHead>
            <TableRow>
              <TableCell>Store ID</TableCell>
              <TableCell>SKU</TableCell>
              <TableCell>Product Name</TableCell>
              <TableCell align="right">Price</TableCell>
              <TableCell>Effective Date</TableCell>
              <TableCell>Last Modified</TableCell>
              <TableCell>Modified By</TableCell>
              <TableCell align="center">Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {results!.items.length === 0 ? (
              <TableRow>
                <TableCell colSpan={8} align="center">
                  No records match your search criteria.
                </TableCell>
              </TableRow>
            ) : (
              results!.items.map((record: PricingRecord) => (
                <TableRow key={record.id} hover>
                  <TableCell>
                    <Chip label={record.storeId} size="small" variant="outlined" />
                  </TableCell>
                  <TableCell>
                    <Typography variant="body2" fontFamily="monospace">
                      {record.sku}
                    </Typography>
                  </TableCell>
                  <TableCell>{record.productName}</TableCell>
                  <TableCell align="right">
                    <Typography variant="body2" fontWeight={600}>
                      {currencyFormatter(record.price, record.currencyCode)}
                    </Typography>
                  </TableCell>
                  <TableCell>{record.effectiveDate}</TableCell>
                  <TableCell>
                    {record.lastModifiedAt
                      ? new Date(record.lastModifiedAt).toLocaleDateString()
                      : "—"}
                  </TableCell>
                  <TableCell>{record.lastModifiedBy || "—"}</TableCell>
                  <TableCell align="center">
                    <Tooltip title="Edit record">
                      <IconButton
                        size="small"
                        color="primary"
                        onClick={() => handleEdit(record)}
                      >
                        <EditIcon fontSize="small" />
                      </IconButton>
                    </Tooltip>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>

      {results!.totalPages > 1 && (
        <Box display="flex" justifyContent="flex-end" mt={2}>
          <Pagination
            count={results!.totalPages}
            page={results!.pageNumber}
            onChange={handlePageChange}
            color="primary"
          />
        </Box>
      )}

      <EditModal authToken={authToken} />
    </Box>
  );
};

export default PricingGrid;
