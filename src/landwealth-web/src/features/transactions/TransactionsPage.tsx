import React, { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import {
  Alert, Button, Checkbox, LinearProgress, MenuItem, Paper, Table, TableBody, TableCell, TableHead, TablePagination, TableRow, TextField, Typography,
} from '@mui/material';
import { apiClient } from '../../api/client';
import { formatIndianRupee } from '../../utils/currencyFormatter';
import { accountFieldLabels, buildJournal, categoryTypesFor, needsCounterAccount, needsProperty, transactionTypeLabel, transactionTypes } from './transactionJournal';

interface TransactionRow {
  id: string;
  transactionDate: string;
  transactionType: string;
  amount: number;
  description: string;
  status: string;
  notes: string | null;
}

interface TransactionPage {
  items: TransactionRow[];
  totalCount: number;
}

interface AccountOption {
  id: string;
  name: string;
  accountType: string;
  isActive: boolean;
}

interface CategoryOption {
  id: string;
  name: string;
  categoryType: string;
}

function problemMessage(error: unknown, fallback: string) {
  const detail = (error as AxiosError<{ detail?: string }>).response?.data?.detail;
  return detail || fallback;
}

export const TransactionsPage: React.FC = () => {
  const client = useQueryClient();
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [propertyFilter, setPropertyFilter] = useState('');
  const [accountFilter, setAccountFilter] = useState('');
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(25);
  const [selected, setSelected] = useState<string[]>([]);
  const [formError, setFormError] = useState<string | null>(null);
  const [reversalNote, setReversalNote] = useState('');

  const resetPage = () => {
    setPage(0);
    setSelected([]);
  };

  const transactions = useQuery({
    queryKey: ['transactions', from, to, propertyFilter, accountFilter, page, pageSize],
    queryFn: async () => (await apiClient.get<TransactionPage>('/api/transactions', {
      params: {
        from: from || undefined,
        to: to || undefined,
        propertyId: propertyFilter || undefined,
        accountId: accountFilter || undefined,
        page: page + 1,
        pageSize,
      },
    })).data,
  });
  const accounts = useQuery({
    queryKey: ['accounts'],
    queryFn: async () => (await apiClient.get<AccountOption[]>('/api/accounts')).data,
  });
  const properties = useQuery({
    queryKey: ['properties'],
    queryFn: async () => (await apiClient.get('/api/properties')).data as { id: string; name: string }[],
  });
  const categories = useQuery({
    queryKey: ['categories'],
    queryFn: async () => (await apiClient.get<CategoryOption[]>('/api/categories')).data,
  });

  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [type, setType] = useState('Expense');
  const [amount, setAmount] = useState('');
  const [description, setDescription] = useState('');
  const [accountId, setAccountId] = useState('');
  const [counterAccountId, setCounterAccountId] = useState('');
  const [propertyId, setPropertyId] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [adjustDate, setAdjustDate] = useState(new Date().toISOString().slice(0, 10));
  const [adjustAmount, setAdjustAmount] = useState('');
  const [adjustDescription, setAdjustDescription] = useState('');
  const [adjustReason, setAdjustReason] = useState('');
  const [adjustAccountId, setAdjustAccountId] = useState('');
  const [adjustLineType, setAdjustLineType] = useState<'Debit' | 'Credit'>('Debit');
  const [adjustCategoryId, setAdjustCategoryId] = useState('');

  const activeAccounts = (accounts.data ?? []).filter((account) => account.isActive);
  const labels = accountFieldLabels(type);
  const allowedCategories = categoryTypesFor(type);
  const visibleCategories = (categories.data ?? []).filter((category) => allowedCategories.includes(category.categoryType));
  const counterAccounts = activeAccounts.filter((account) => {
    if (account.id === accountId) return false;
    if (type === 'Investment') return account.accountType === 'OtherFinancialAccount';
    if (type === 'LiabilityPayment') return account.accountType === 'CreditCard';
    return account.accountType !== 'CreditCard';
  });

  const refresh = () => {
    client.invalidateQueries({ queryKey: ['transactions'] });
    client.invalidateQueries({ queryKey: ['accounts'] });
    client.invalidateQueries({ queryKey: ['account-statement'] });
    client.invalidateQueries({ queryKey: ['dashboard'] });
  };

  const ready = Boolean(description && amount && accountId)
    && (!needsProperty(type) || Boolean(propertyId))
    && (!needsCounterAccount(type) || Boolean(counterAccountId))
    && (needsCounterAccount(type) || Boolean(categoryId));

  const create = useMutation({
    mutationFn: () => {
      const value = Number(amount);
      return apiClient.post('/api/transactions', {
        transactionDate: date,
        transactionType: type,
        amount: value,
        description,
        propertyId: needsProperty(type) ? propertyId : null,
        lines: buildJournal({
          type,
          amount: value,
          accountId,
          counterAccountId,
          propertyId: needsProperty(type) ? propertyId : undefined,
          categoryId,
        }),
      });
    },
    onSuccess: () => {
      setAmount('');
      setDescription('');
      setFormError(null);
      refresh();
    },
    onError: (error) => setFormError(problemMessage(error, 'The entry was rejected.')),
  });

  const rows = transactions.data?.items ?? [];
  const rowIds = rows.map((transaction) => transaction.id);
  const allOnPage = rowIds.length > 0 && rowIds.every((id) => selected.includes(id));
  const someOnPage = rowIds.some((id) => selected.includes(id));

  const toggleAll = () => {
    setSelected(allOnPage
      ? selected.filter((id) => !rowIds.includes(id))
      : [...new Set([...selected, ...rowIds])]);
  };

  const toggleOne = (id: string) => {
    setSelected(selected.includes(id) ? selected.filter((item) => item !== id) : [...selected, id]);
  };

  const exportExcel = useMutation({
    mutationFn: async () => {
      const response = await apiClient.post('/api/transactions/export', { ids: selected }, { responseType: 'blob' });
      const url = URL.createObjectURL(response.data);
      const link = document.createElement('a');
      link.href = url;
      link.download = 'transactions.xlsx';
      link.click();
      URL.revokeObjectURL(url);
    },
    onSuccess: () => setFormError(null),
    onError: (error) => setFormError(problemMessage(error, 'The selected transactions could not be exported.')),
  });

  const reverse = useMutation({
    mutationFn: (id: string) => apiClient.post(`/api/transactions/${id}/reverse`, { description: reversalNote }),
    onSuccess: () => {
      setReversalNote('');
      setFormError(null);
      refresh();
    },
    onError: (error) => setFormError(problemMessage(error, 'The reversal was rejected.')),
  });

  const adjust = useMutation({
    mutationFn: () => {
      const value = Number(adjustAmount);
      const offset = adjustLineType === 'Debit' ? 'Credit' : 'Debit';
      return apiClient.post('/api/transactions/adjustments', {
        transactionDate: adjustDate,
        amount: value,
        description: adjustDescription,
        reason: adjustReason,
        lines: [
          { lineType: adjustLineType, amount: value, accountId: adjustAccountId },
          { lineType: offset, amount: value, categoryId: adjustCategoryId },
        ],
      });
    },
    onSuccess: () => {
      setAdjustAmount('');
      setAdjustDescription('');
      setAdjustReason('');
      setFormError(null);
      refresh();
    },
    onError: (error) => setFormError(problemMessage(error, 'The adjustment was rejected.')),
  });

  return (
    <>
      <Typography variant="h4" gutterBottom>Transactions</Typography>
      <Typography color="text.secondary" sx={{ mb: 2 }}>
        Posted entries stay in the ledger. Correct a mistake with a reversal or an adjustment.
      </Typography>
      <Paper sx={{ p: 2, mb: 2, display: 'grid', gap: 2, gridTemplateColumns: { md: 'repeat(4, 1fr)' } }}>
        <TextField type="date" label="From" InputLabelProps={{ shrink: true }} value={from} onChange={(event) => { setFrom(event.target.value); resetPage(); }} />
        <TextField type="date" label="To" InputLabelProps={{ shrink: true }} value={to} onChange={(event) => { setTo(event.target.value); resetPage(); }} />
        <TextField select label="Property" value={propertyFilter} onChange={(event) => { setPropertyFilter(event.target.value); resetPage(); }}>
          <MenuItem value="">All</MenuItem>
          {(properties.data ?? []).map((property) => <MenuItem key={property.id} value={property.id}>{property.name}</MenuItem>)}
        </TextField>
        <TextField select label="Account" value={accountFilter} onChange={(event) => { setAccountFilter(event.target.value); resetPage(); }}>
          <MenuItem value="">All</MenuItem>
          {(accounts.data ?? []).map((account) => <MenuItem key={account.id} value={account.id}>{account.name}</MenuItem>)}
        </TextField>
      </Paper>
      {transactions.isLoading && <LinearProgress />}
      {transactions.isError && <Alert severity="error">The ledger could not be loaded.</Alert>}
      {transactions.data && (
        <Paper sx={{ mb: 3 }}>
          <Button
            sx={{ m: 2 }}
            variant="outlined"
            disabled={selected.length === 0 || exportExcel.isPending}
            onClick={() => exportExcel.mutate()}
          >
            Export selected ({selected.length})
          </Button>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell padding="checkbox">
                  <Checkbox
                    checked={allOnPage}
                    indeterminate={!allOnPage && someOnPage}
                    onChange={toggleAll}
                    inputProps={{ 'aria-label': 'Select transactions on this page' }}
                  />
                </TableCell>
                <TableCell>Date</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Description</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Amount</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.length === 0 && (
                <TableRow><TableCell colSpan={7}>No transactions for this filter.</TableCell></TableRow>
              )}
              {rows.map((transaction) => (
                <TableRow key={transaction.id} selected={selected.includes(transaction.id)}>
                  <TableCell padding="checkbox">
                    <Checkbox
                      checked={selected.includes(transaction.id)}
                      onChange={() => toggleOne(transaction.id)}
                      inputProps={{ 'aria-label': `Select ${transaction.description}` }}
                    />
                  </TableCell>
                  <TableCell>{transaction.transactionDate}</TableCell>
                  <TableCell>{transactionTypeLabel(transaction.transactionType)}</TableCell>
                  <TableCell>
                    {transaction.description}
                    {transaction.notes && <Typography variant="caption" display="block">{transaction.notes}</Typography>}
                  </TableCell>
                  <TableCell>{transaction.status}</TableCell>
                  <TableCell align="right">{formatIndianRupee(transaction.amount)}</TableCell>
                  <TableCell>
                    {transaction.status === 'Posted' && transaction.transactionType !== 'Reversal' && (
                      <Button size="small" disabled={!reversalNote || reverse.isPending} onClick={() => reverse.mutate(transaction.id)}>
                        Reverse
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <TablePagination
            component="div"
            count={transactions.data.totalCount}
            page={page}
            onPageChange={(_, nextPage) => setPage(nextPage)}
            rowsPerPage={pageSize}
            onRowsPerPageChange={(event) => {
              setPageSize(Number(event.target.value));
              setPage(0);
            }}
            rowsPerPageOptions={[10, 25, 50]}
          />
          <TextField
            label="Reversal note"
            value={reversalNote}
            onChange={(event) => setReversalNote(event.target.value)}
            sx={{ m: 2, width: { md: 360 } }}
            helperText="Required before a posted entry can be reversed."
          />
        </Paper>
      )}

      <Typography variant="h6">New posted entry</Typography>
      <Paper sx={{ p: 2, display: 'grid', gap: 2, gridTemplateColumns: { md: 'repeat(3, 1fr)' } }}>
        <TextField type="date" label="Date" InputLabelProps={{ shrink: true }} value={date} onChange={(event) => setDate(event.target.value)} />
        <TextField select label="Type" value={type} onChange={(event) => { setType(event.target.value); setCategoryId(''); setCounterAccountId(''); }}>
          {transactionTypes.map((item) => <MenuItem key={item.value} value={item.value}>{item.label}</MenuItem>)}
        </TextField>
        <TextField label="Amount" value={amount} onChange={(event) => setAmount(event.target.value)} />
        <TextField label="Description" value={description} onChange={(event) => setDescription(event.target.value)} />
        <TextField select label={labels.account} value={accountId} onChange={(event) => setAccountId(event.target.value)}>
          {activeAccounts.map((account) => <MenuItem key={account.id} value={account.id}>{account.name}</MenuItem>)}
        </TextField>
        {needsCounterAccount(type) && (
          <TextField select label={labels.counter} value={counterAccountId} onChange={(event) => setCounterAccountId(event.target.value)}>
            {counterAccounts.map((account) => <MenuItem key={account.id} value={account.id}>{account.name}</MenuItem>)}
          </TextField>
        )}
        {needsProperty(type) && (
          <TextField select label="Property" value={propertyId} onChange={(event) => setPropertyId(event.target.value)}>
            {(properties.data ?? []).map((property) => <MenuItem key={property.id} value={property.id}>{property.name}</MenuItem>)}
          </TextField>
        )}
        {!needsCounterAccount(type) && (
          <TextField select label="Category" value={categoryId} onChange={(event) => setCategoryId(event.target.value)}>
            {visibleCategories.map((category) => <MenuItem key={category.id} value={category.id}>{category.name}</MenuItem>)}
          </TextField>
        )}
        <Button variant="contained" disabled={!ready || create.isPending} onClick={() => create.mutate()}>Post</Button>
      </Paper>

      <Typography variant="h6" sx={{ mt: 3 }}>Adjustment</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
        A bank or cash debit increases that balance. A credit card credit increases the amount owed. The offset line uses the opposite entry.
      </Typography>
      <Paper sx={{ p: 2, display: 'grid', gap: 2, gridTemplateColumns: { md: 'repeat(3, 1fr)' } }}>
        <TextField type="date" label="Date" InputLabelProps={{ shrink: true }} value={adjustDate} onChange={(event) => setAdjustDate(event.target.value)} />
        <TextField label="Amount" value={adjustAmount} onChange={(event) => setAdjustAmount(event.target.value)} />
        <TextField label="Description" value={adjustDescription} onChange={(event) => setAdjustDescription(event.target.value)} />
        <TextField label="Reason" value={adjustReason} onChange={(event) => setAdjustReason(event.target.value)} />
        <TextField select label="Account" value={adjustAccountId} onChange={(event) => setAdjustAccountId(event.target.value)}>
          {activeAccounts.map((account) => <MenuItem key={account.id} value={account.id}>{account.name}</MenuItem>)}
        </TextField>
        <TextField select label="Account entry" value={adjustLineType} onChange={(event) => setAdjustLineType(event.target.value as 'Debit' | 'Credit')}>
          <MenuItem value="Debit">Debit</MenuItem>
          <MenuItem value="Credit">Credit</MenuItem>
        </TextField>
        <TextField select label="Offset category" value={adjustCategoryId} onChange={(event) => setAdjustCategoryId(event.target.value)}>
          {(categories.data ?? []).map((category) => <MenuItem key={category.id} value={category.id}>{category.name}</MenuItem>)}
        </TextField>
        <Button
          variant="contained"
          disabled={!adjustDescription || !adjustReason || !adjustAmount || !adjustAccountId || !adjustCategoryId || adjust.isPending}
          onClick={() => adjust.mutate()}
        >
          Post adjustment
        </Button>
      </Paper>
      {formError && <Alert severity="error" sx={{ mt: 2 }}>{formError}</Alert>}
    </>
  );
};
