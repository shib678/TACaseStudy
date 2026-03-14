import React, { useEffect } from "react";
import {
  Box, Chip, CircularProgress, Paper, Table, TableBody,
  TableCell, TableContainer, TableHead, TableRow, Typography,
} from "@mui/material";
import { useDispatch, useSelector } from "react-redux";
import { fetchUploadHistory } from "../store/uploadSlice";

interface UploadHistoryProps {
  storeId: string;
  authToken?: string;
}

const statusColor = (status: string) => {
  switch (status) {
    case "Completed": return "success";
    case "CompletedWithErrors": return "warning";
    case "Failed": return "error";
    case "Processing": return "info";
    default: return "default";
  }
};

const UploadHistory: React.FC<UploadHistoryProps> = ({ storeId, authToken = "" }) => {
  const dispatch = useDispatch<any>();
  const { history, historyLoading } = useSelector((state: any) => state.upload);

  useEffect(() => {
    if (storeId) {
      dispatch(fetchUploadHistory({ storeId, token: authToken }));
    }
  }, [storeId, authToken, dispatch]);

  if (!storeId) return null;

  return (
    <Box mt={4}>
      <Typography variant="h6" gutterBottom>
        Recent Upload History
      </Typography>
      {historyLoading ? (
        <CircularProgress size={24} />
      ) : (
        <TableContainer component={Paper} elevation={2}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>File Name</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Total</TableCell>
                <TableCell align="right">Processed</TableCell>
                <TableCell align="right">Failed</TableCell>
                <TableCell>Uploaded By</TableCell>
                <TableCell>Date</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {history.length === 0 ? (
                <TableRow>
                  <TableCell colSpan={7} align="center">
                    No upload history found.
                  </TableCell>
                </TableRow>
              ) : (
                history.map((row: any) => (
                  <TableRow key={row.id} hover>
                    <TableCell>{row.fileName}</TableCell>
                    <TableCell>
                      <Chip
                        label={row.status}
                        size="small"
                        color={statusColor(row.status) as any}
                      />
                    </TableCell>
                    <TableCell align="right">{row.totalRows}</TableCell>
                    <TableCell align="right">{row.processedRows}</TableCell>
                    <TableCell align="right">{row.failedRows}</TableCell>
                    <TableCell>{row.createdBy}</TableCell>
                    <TableCell>
                      {new Date(row.createdAt).toLocaleString()}
                    </TableCell>
                  </TableRow>
                ))
              )}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Box>
  );
};

export default UploadHistory;
