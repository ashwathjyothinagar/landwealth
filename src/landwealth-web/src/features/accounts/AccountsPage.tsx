import React, { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import {
  Alert, Button, Card, CardContent, Chip, Grid, LinearProgress, MenuItem, TextField, Typography,
} from '@mui/material';
import { apiClient } from '../../api/client';
import { formatIndianRupee } from '../../utils/currencyFormatter';
import { accountTypeLabel, accountTypes, balanceCaption, requiresLastFour } from './accountLabels';

export interface Account {
  id: string;
  name: string;
  accountType: string;
  institution: string | null;
  maskedAccountNumber: string | null;
  currentBalance: number;
  openingBalance: number;
  currency: string;
  isActive: boolean;
  notes: string | null;
}

function problemMessage(error: unknown, fallback: string) {
  const detail = (error as AxiosError<{ detail?: string }>).response?.data?.detail;
  return detail || fallback;
}

export const AccountsPage: React.FC = () => {
  const client = useQueryClient();
  const query = useQuery({
    queryKey: ['accounts'],
    queryFn: async () => (await apiClient.get<Account[]>('/api/accounts')).data,
  });
  const [name, setName] = useState('');
  const [accountType, setAccountType] = useState('BankAccount');
  const [openingBalance, setOpeningBalance] = useState('0');
  const [institution, setInstitution] = useState('');
  const [lastFourDigits, setLastFourDigits] = useState('');
  const [notes, setNotes] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  const maskRequired = requiresLastFour(accountType);
  const digitsReady = !maskRequired || /^\d{4}$/.test(lastFourDigits);
  const create = useMutation({
    mutationFn: () => apiClient.post('/api/accounts', {
      name,
      accountType,
      openingBalance: Number(openingBalance),
      institution: institution || null,
      lastFourDigits: lastFourDigits || null,
      notes: notes || null,
    }),
    onSuccess: () => {
      setName('');
      setLastFourDigits('');
      setNotes('');
      setFormError(null);
      client.invalidateQueries({ queryKey: ['accounts'] });
    },
    onError: (error) => setFormError(problemMessage(error, 'Check the last four digits and opening balance.')),
  });

  return (
    <>
      <Typography variant="h4" gutterBottom>Accounts</Typography>
      <Typography color="text.secondary" sx={{ mb: 2 }}>
        Bank accounts and credit cards keep only the last four digits. Cash drawers do not store a number.
      </Typography>
      {query.isLoading && <LinearProgress />}
      {query.isError && <Alert severity="error">Accounts could not be loaded.</Alert>}
      <Grid container spacing={2}>
        {(query.data ?? []).map((account) => (
          <Grid item xs={12} md={4} key={account.id}>
            <Card>
              <CardContent>
                <Chip size="small" label={accountTypeLabel(account.accountType)} sx={{ mb: 1 }} />
                {!account.isActive && <Chip size="small" label="Inactive" sx={{ mb: 1, ml: 1 }} />}
                <Typography variant="h6">{account.name}</Typography>
                <Typography color="text.secondary">
                  {[account.institution, account.maskedAccountNumber].filter(Boolean).join(' ') || accountTypeLabel(account.accountType)}
                </Typography>
                <Typography variant="caption" color="text.secondary">{balanceCaption(account.accountType)}</Typography>
                <Typography variant="h5">{formatIndianRupee(account.currentBalance)}</Typography>
                <Typography variant="caption">Opening {formatIndianRupee(account.openingBalance)}</Typography>
                <Button component={RouterLink} to={`/accounts/${account.id}`} size="small" sx={{ display: 'block', mt: 1 }}>
                  Manage
                </Button>
              </CardContent>
            </Card>
          </Grid>
        ))}
        {query.data?.length === 0 && (
          <Grid item xs={12}>
            <Alert severity="info">Add a bank account, cash drawer, or credit card to start the ledger.</Alert>
          </Grid>
        )}
      </Grid>
      <Typography variant="h6" sx={{ mt: 3 }}>Add account</Typography>
      <Grid container spacing={2} sx={{ mt: 0 }}>
        <Grid item xs={12} md={4}>
          <TextField label="Name" fullWidth value={name} onChange={(event) => setName(event.target.value)} />
        </Grid>
        <Grid item xs={12} md={4}>
          <TextField select label="Type" fullWidth value={accountType} onChange={(event) => setAccountType(event.target.value)}>
            {accountTypes.map((type) => <MenuItem key={type.value} value={type.value}>{type.label}</MenuItem>)}
          </TextField>
        </Grid>
        <Grid item xs={12} md={4}>
          <TextField label="Institution" fullWidth value={institution} onChange={(event) => setInstitution(event.target.value)} />
        </Grid>
        <Grid item xs={12} md={4}>
          <TextField
            label={maskRequired ? 'Last 4 digits' : 'Last 4 digits (optional)'}
            fullWidth
            value={lastFourDigits}
            onChange={(event) => setLastFourDigits(event.target.value.replace(/\D/g, '').slice(0, 4))}
            inputProps={{ maxLength: 4, inputMode: 'numeric' }}
            helperText="A full account number is never stored."
          />
        </Grid>
        <Grid item xs={12} md={4}>
          <TextField
            label={accountType === 'CreditCard' ? 'Opening amount owed' : 'Opening balance'}
            fullWidth
            value={openingBalance}
            onChange={(event) => setOpeningBalance(event.target.value)}
          />
        </Grid>
        <Grid item xs={12} md={4}>
          <TextField label="Notes" fullWidth value={notes} onChange={(event) => setNotes(event.target.value)} />
        </Grid>
        <Grid item xs={12}>
          <Button variant="contained" disabled={!name || !digitsReady || create.isPending} onClick={() => create.mutate()}>
            Add account
          </Button>
        </Grid>
      </Grid>
      {formError && <Alert severity="error" sx={{ mt: 2 }}>{formError}</Alert>}
    </>
  );
};
