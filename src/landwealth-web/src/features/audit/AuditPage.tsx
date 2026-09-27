import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { Alert, LinearProgress, Paper, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { apiClient } from '../../api/client';

export interface AuditEntry {
  id: number;
  entityName: string;
  entityId: string;
  action: string;
  timestamp: string;
  summary: string | null;
}

export const AuditLogTable: React.FC<{ entries: AuditEntry[] }> = ({ entries }) => (
  <Table size="small">
    <TableHead>
      <TableRow>
        <TableCell>When</TableCell>
        <TableCell>Action</TableCell>
        <TableCell>Record</TableCell>
        <TableCell>Summary</TableCell>
      </TableRow>
    </TableHead>
    <TableBody>
      {entries.length === 0 && (
        <TableRow><TableCell colSpan={4}>No financial changes recorded yet.</TableCell></TableRow>
      )}
      {entries.map((entry) => (
        <TableRow key={entry.id} data-testid={`audit-${entry.action}`}>
          <TableCell>{new Date(entry.timestamp).toLocaleString('en-IN')}</TableCell>
          <TableCell>{entry.action}</TableCell>
          <TableCell>{entry.entityName}</TableCell>
          <TableCell>{entry.summary ?? '—'}</TableCell>
        </TableRow>
      ))}
    </TableBody>
  </Table>
);

export const AuditPage: React.FC = () => {
  const logs = useQuery({
    queryKey: ['audit-logs'],
    queryFn: async () => (await apiClient.get<AuditEntry[]>('/api/audit-logs')).data,
  });

  return (
    <>
      <Typography variant="h4" gutterBottom>Audit log</Typography>
      <Typography color="text.secondary" sx={{ mb: 2 }}>
        Posted financial changes stay in this log. A reversal is recorded beside the original entry.
      </Typography>
      {logs.isLoading && <LinearProgress />}
      {logs.isError && <Alert severity="error">The audit log could not be loaded.</Alert>}
      {logs.data && (
        <Paper>
          <AuditLogTable entries={logs.data} />
        </Paper>
      )}
    </>
  );
};
