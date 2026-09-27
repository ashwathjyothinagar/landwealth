import React, { useEffect, useState } from 'react';
import { Link as RouterLink, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import {
  Alert, Button, Chip, Grid, LinearProgress, Switch, FormControlLabel, Table, TableBody, TableCell,
  TableHead, TableRow, TextField, Typography,
} from '@mui/material';
import { apiClient } from '../../api/client';
import { formatIndianRupee } from '../../utils/currencyFormatter';
import { accountTypeLabel, balanceCaption } from './accountLabels';
import type { Account } from './AccountsPage';

interface Activity {
  transactionId: string;
  date: string;
  description: string;
  lineType: string;
  amount: number;
  balance: number;
}

interface Statement {
  account: Account;
  activity: Activity[];
}

function problemMessage(error: unknown, fallback: string) {
  const detail = (error as AxiosError<{ detail?: string }>).response?.data?.detail;
  return detail || fallback;
}

export const AccountDetailPage: React.FC = () => {
  const { id = '' } = useParams();
  const client = useQueryClient();
  const statement = useQuery({
    queryKey: ['account-statement', id],
    queryFn: async () => (await apiClient.get<Statement>(`/api/accounts/${id}/statement`)).data,
  });
  const account = statement.data?.account;
  const [name, setName] = useState('');
  const [institution, setInstitution] = useState('');
  const [notes, setNotes] = useState('');
  const [isActive, setIsActive] = useState(true);
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!account) return;
    setName(account.name);
    setInstitution(account.institution ?? '');
    setNotes(account.notes ?? '');
    setIsActive(account.isActive);
  }, [account]);

  const save = useMutation({
    mutationFn: () => apiClient.put(`/api/accounts/${id}`, {
      name,
      institution: institution || null,
      notes: notes || null,
      isActive,
    }),
    onSuccess: () => {
      setFormError(null);
      client.invalidateQueries({ queryKey: ['account-statement', id] });
      client.invalidateQueries({ queryKey: ['accounts'] });
    },
    onError: (error) => setFormError(problemMessage(error, 'The account could not be updated.')),
  });

  const activity = statement.data?.activity;
  if (statement.isLoading) return <LinearProgress />;
  if (statement.isError || !account || !activity) return <Alert severity="error">This account could not be loaded.</Alert>;

  return (
    <>
      <Button component={RouterLink} to="/accounts" sx={{ mb: 1 }}>Back to accounts</Button>
      <Typography variant="h4" gutterBottom>{account.name}</Typography>
      <Chip label={accountTypeLabel(account.accountType)} sx={{ mr: 1 }} />
      <Chip label={account.isActive ? 'Active' : 'Inactive'} />
      <Typography color="text.secondary" sx={{ mt: 1 }}>
        {[account.institution, account.maskedAccountNumber].filter(Boolean).join(' ')}
      </Typography>
      <Typography variant="caption" color="text.secondary">{balanceCaption(account.accountType)}</Typography>
      <Typography variant="h5">{formatIndianRupee(account.currentBalance)}</Typography>
      <Typography variant="body2">Opening {formatIndianRupee(account.openingBalance)}</Typography>

      <Typography variant="h6" sx={{ mt: 3 }}>Activity</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
        Each balance is the running total after that entry, starting from the opening balance.
      </Typography>
      {activity.length === 0 && <Alert severity="info">No entries yet. The balance is still the opening balance.</Alert>}
      {activity.length > 0 && (
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Date</TableCell>
              <TableCell>Description</TableCell>
              <TableCell>Entry</TableCell>
              <TableCell align="right">Amount</TableCell>
              <TableCell align="right">{balanceCaption(account.accountType)}</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {activity.map((row) => (
              <TableRow key={`${row.transactionId}-${row.lineType}-${row.amount}`}>
                <TableCell>{row.date}</TableCell>
                <TableCell>{row.description}</TableCell>
                <TableCell>{row.lineType}</TableCell>
                <TableCell align="right">{formatIndianRupee(row.amount)}</TableCell>
                <TableCell align="right">{formatIndianRupee(row.balance)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}

      <Typography variant="h6" sx={{ mt: 3 }}>Manage</Typography>
      <Grid container spacing={2} sx={{ mt: 0 }}>
        <Grid item xs={12} md={4}>
          <TextField label="Name" fullWidth value={name} onChange={(event) => setName(event.target.value)} />
        </Grid>
        <Grid item xs={12} md={4}>
          <TextField label="Institution" fullWidth value={institution} onChange={(event) => setInstitution(event.target.value)} />
        </Grid>
        <Grid item xs={12} md={4}>
          <TextField label="Notes" fullWidth value={notes} onChange={(event) => setNotes(event.target.value)} />
        </Grid>
        <Grid item xs={12}>
          <FormControlLabel
            control={<Switch checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />}
            label="Active"
          />
        </Grid>
        <Grid item xs={12}>
          <Button variant="contained" disabled={!name || save.isPending} onClick={() => save.mutate()}>Save</Button>
        </Grid>
      </Grid>
      {formError && <Alert severity="error" sx={{ mt: 2 }}>{formError}</Alert>}
      {save.isSuccess && <Alert severity="success" sx={{ mt: 2 }}>Account updated.</Alert>}
    </>
  );
};
