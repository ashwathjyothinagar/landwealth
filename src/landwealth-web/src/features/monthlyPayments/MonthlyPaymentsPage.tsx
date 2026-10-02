import React, { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import {
  Alert, Button, Chip, LinearProgress, MenuItem, Paper, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography,
} from '@mui/material';
import { apiClient } from '../../api/client';
import { formatIndianRupee } from '../../utils/currencyFormatter';

interface MonthlyPayment {
  id: string;
  name: string;
  kind: string;
  amount: number;
  dueDay: number;
  isActive: boolean;
  totalInstallments: number | null;
  startsOn: string;
}

interface MonthItem {
  id: string;
  name: string;
  kind: string;
  expectedAmount: number;
  dueDate: string;
  paid: boolean;
  paidOn: string | null;
  paidAmount: number | null;
  installmentsRemaining: number | null;
}

interface MonthTransaction {
  transactionId: string;
  name: string;
  paidOn: string;
  amount: number;
  description: string;
}

interface MonthBoard {
  dueCount: number;
  paidCount: number;
  remainingCount: number;
  dueAmount: number;
  paidAmount: number;
  remainingAmount: number;
  items: MonthItem[];
  transactions: MonthTransaction[];
}

interface AccountOption {
  id: string;
  name: string;
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

function currentMonth() {
  return new Date().toISOString().slice(0, 7);
}

export const MonthlyPaymentsPage: React.FC = () => {
  const client = useQueryClient();
  const [month, setMonth] = useState(currentMonth);
  const [formError, setFormError] = useState<string | null>(null);
  const [name, setName] = useState('');
  const [kind, setKind] = useState('Bill');
  const [amount, setAmount] = useState('');
  const [dueDay, setDueDay] = useState('5');
  const [accountId, setAccountId] = useState('');
  const [categoryId, setCategoryId] = useState('');
  const [startsOn, setStartsOn] = useState(currentMonth);
  const [installments, setInstallments] = useState('');
  const [drafts, setDrafts] = useState<Record<string, { date: string; amount: string }>>({});

  const [year, monthNumber] = month.split('-').map(Number);

  const board = useQuery({
    queryKey: ['monthly-payments-month', year, monthNumber],
    queryFn: async () => (await apiClient.get<MonthBoard>('/api/monthly-payments/month', { params: { year, month: monthNumber } })).data,
  });
  const payments = useQuery({
    queryKey: ['monthly-payments'],
    queryFn: async () => (await apiClient.get<MonthlyPayment[]>('/api/monthly-payments')).data,
  });
  const accounts = useQuery({
    queryKey: ['accounts'],
    queryFn: async () => (await apiClient.get<AccountOption[]>('/api/accounts')).data,
  });
  const categories = useQuery({
    queryKey: ['categories'],
    queryFn: async () => (await apiClient.get<CategoryOption[]>('/api/categories')).data,
  });

  const refresh = () => {
    client.invalidateQueries({ queryKey: ['monthly-payments'] });
    client.invalidateQueries({ queryKey: ['monthly-payments-month'] });
    client.invalidateQueries({ queryKey: ['transactions'] });
    client.invalidateQueries({ queryKey: ['accounts'] });
    client.invalidateQueries({ queryKey: ['dashboard'] });
  };

  const create = useMutation({
    mutationFn: () => apiClient.post('/api/monthly-payments', {
      name,
      kind,
      amount: Number(amount),
      dueDay: Number(dueDay),
      accountId,
      categoryId,
      startsOn: `${startsOn}-01`,
      totalInstallments: kind === 'Emi' ? Number(installments) : null,
      notes: null,
    }),
    onSuccess: () => {
      setName('');
      setAmount('');
      setInstallments('');
      setFormError(null);
      refresh();
    },
    onError: (error) => setFormError(problemMessage(error, 'The payment could not be added.')),
  });

  const record = useMutation({
    mutationFn: (item: MonthItem) => {
      const draft = drafts[item.id] ?? { date: item.dueDate, amount: String(item.expectedAmount) };
      return apiClient.post(`/api/monthly-payments/${item.id}/payments`, {
        year,
        month: monthNumber,
        paidOn: draft.date,
        amount: Number(draft.amount),
      });
    },
    onSuccess: () => {
      setFormError(null);
      refresh();
    },
    onError: (error) => setFormError(problemMessage(error, 'The payment could not be recorded.')),
  });

  const stop = useMutation({
    mutationFn: (id: string) => apiClient.post(`/api/monthly-payments/${id}/stop`),
    onSuccess: refresh,
    onError: (error) => setFormError(problemMessage(error, 'The payment could not be stopped.')),
  });

  const activeAccounts = (accounts.data ?? []).filter((account) => account.isActive);
  const expenseCategories = (categories.data ?? []).filter((category) => category.categoryType === 'Expense');
  const ready = Boolean(name && amount && accountId && categoryId && dueDay)
    && (kind !== 'Emi' || Boolean(installments));

  const draftFor = (item: MonthItem) => drafts[item.id] ?? { date: item.dueDate, amount: String(item.expectedAmount) };

  return (
    <>
      <Typography variant="h4" gutterBottom>Monthly payments</Typography>
      <Typography color="text.secondary" sx={{ mb: 2 }}>
        EMIs and bills you clear every month. Choose a month to see what was paid, the date, and what is still open.
      </Typography>
      <TextField
        type="month"
        label="Month"
        InputLabelProps={{ shrink: true }}
        value={month}
        onChange={(event) => setMonth(event.target.value)}
        sx={{ mb: 2 }}
      />
      {board.isLoading && <LinearProgress />}
      {board.isError && <Alert severity="error">The month could not be loaded.</Alert>}
      {board.data && (
        <>
          <Paper sx={{ p: 2, mb: 2, display: 'flex', gap: 1, flexWrap: 'wrap' }}>
            <Chip label={`${board.data.dueCount} due`} />
            <Chip label={`${board.data.paidCount} paid`} color="success" />
            <Chip label={`${board.data.remainingCount} remaining`} color={board.data.remainingCount > 0 ? 'warning' : 'default'} />
            <Chip label={`Paid ${formatIndianRupee(board.data.paidAmount)}`} />
            <Chip label={`Still to pay ${formatIndianRupee(board.data.remainingAmount)}`} />
          </Paper>
          <Typography variant="h6">This month</Typography>
          <Paper sx={{ mb: 3 }}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Payment</TableCell>
                  <TableCell>Kind</TableCell>
                  <TableCell>Due</TableCell>
                  <TableCell align="right">Expected</TableCell>
                  <TableCell>Status</TableCell>
                  <TableCell>Installments left</TableCell>
                  <TableCell>Record</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {board.data.items.length === 0 && (
                  <TableRow><TableCell colSpan={7}>Nothing is due in this month.</TableCell></TableRow>
                )}
                {board.data.items.map((item) => {
                  const draft = draftFor(item);
                  return (
                    <TableRow key={item.id}>
                      <TableCell>{item.name}</TableCell>
                      <TableCell>{item.kind === 'Emi' ? 'EMI' : 'Bill'}</TableCell>
                      <TableCell>{item.dueDate}</TableCell>
                      <TableCell align="right">{formatIndianRupee(item.expectedAmount)}</TableCell>
                      <TableCell>
                        {item.paid
                          ? `Paid ${formatIndianRupee(item.paidAmount ?? 0)} on ${item.paidOn}`
                          : 'Remaining'}
                      </TableCell>
                      <TableCell>{item.installmentsRemaining ?? '—'}</TableCell>
                      <TableCell>
                        {!item.paid && (
                          <>
                            <TextField
                              type="date"
                              size="small"
                              value={draft.date}
                              onChange={(event) => setDrafts({ ...drafts, [item.id]: { ...draft, date: event.target.value } })}
                              sx={{ mr: 1, width: 160 }}
                            />
                            <TextField
                              size="small"
                              value={draft.amount}
                              onChange={(event) => setDrafts({ ...drafts, [item.id]: { ...draft, amount: event.target.value } })}
                              sx={{ mr: 1, width: 120 }}
                            />
                            <Button size="small" variant="contained" disabled={record.isPending || !draft.date || !draft.amount} onClick={() => record.mutate(item)}>
                              Add transaction
                            </Button>
                          </>
                        )}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          </Paper>
          <Typography variant="h6">Transactions</Typography>
          <Paper sx={{ mb: 3 }}>
            <Table>
              <TableHead>
                <TableRow>
                  <TableCell>Date</TableCell>
                  <TableCell>Payment</TableCell>
                  <TableCell>Description</TableCell>
                  <TableCell align="right">Amount</TableCell>
                </TableRow>
              </TableHead>
              <TableBody>
                {board.data.transactions.length === 0 && (
                  <TableRow><TableCell colSpan={4}>No transactions for this month.</TableCell></TableRow>
                )}
                {board.data.transactions.map((transaction) => (
                  <TableRow key={transaction.transactionId}>
                    <TableCell>{transaction.paidOn}</TableCell>
                    <TableCell>{transaction.name}</TableCell>
                    <TableCell>{transaction.description}</TableCell>
                    <TableCell align="right">{formatIndianRupee(transaction.amount)}</TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Paper>
        </>
      )}

      <Typography variant="h6">The set</Typography>
      <Paper sx={{ mb: 3 }}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Payment</TableCell>
              <TableCell>Kind</TableCell>
              <TableCell>Due day</TableCell>
              <TableCell align="right">Amount</TableCell>
              <TableCell>Starts</TableCell>
              <TableCell>Status</TableCell>
              <TableCell />
            </TableRow>
          </TableHead>
          <TableBody>
            {(payments.data ?? []).length === 0 && (
              <TableRow><TableCell colSpan={7}>Add the EMIs and bills you pay each month.</TableCell></TableRow>
            )}
            {(payments.data ?? []).map((payment) => (
              <TableRow key={payment.id}>
                <TableCell>{payment.name}</TableCell>
                <TableCell>{payment.kind === 'Emi' ? 'EMI' : 'Bill'}</TableCell>
                <TableCell>{payment.dueDay}</TableCell>
                <TableCell align="right">{formatIndianRupee(payment.amount)}</TableCell>
                <TableCell>{payment.startsOn}</TableCell>
                <TableCell>{payment.isActive ? 'Active' : 'Stopped'}</TableCell>
                <TableCell>
                  {payment.isActive && (
                    <Button size="small" disabled={stop.isPending} onClick={() => stop.mutate(payment.id)}>Stop</Button>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>

      <Typography variant="h6">Add to the set</Typography>
      <Paper sx={{ p: 2, display: 'grid', gap: 2, gridTemplateColumns: { md: 'repeat(3, 1fr)' } }}>
        <TextField label="Name" value={name} onChange={(event) => setName(event.target.value)} />
        <TextField select label="Kind" value={kind} onChange={(event) => setKind(event.target.value)}>
          <MenuItem value="Bill">Bill</MenuItem>
          <MenuItem value="Emi">EMI</MenuItem>
        </TextField>
        <TextField label="Amount" value={amount} onChange={(event) => setAmount(event.target.value)} />
        <TextField label="Due day" value={dueDay} onChange={(event) => setDueDay(event.target.value)} />
        <TextField select label="Paid from" value={accountId} onChange={(event) => setAccountId(event.target.value)}>
          {activeAccounts.map((account) => <MenuItem key={account.id} value={account.id}>{account.name}</MenuItem>)}
        </TextField>
        <TextField select label="Category" value={categoryId} onChange={(event) => setCategoryId(event.target.value)}>
          {expenseCategories.map((category) => <MenuItem key={category.id} value={category.id}>{category.name}</MenuItem>)}
        </TextField>
        <TextField type="month" label="First month" InputLabelProps={{ shrink: true }} value={startsOn} onChange={(event) => setStartsOn(event.target.value)} />
        {kind === 'Emi' && (
          <TextField label="Installments" value={installments} onChange={(event) => setInstallments(event.target.value)} />
        )}
        <Button variant="contained" disabled={!ready || create.isPending} onClick={() => create.mutate()}>Add payment</Button>
      </Paper>
      {formError && <Alert severity="error" sx={{ mt: 2 }}>{formError}</Alert>}
    </>
  );
};
