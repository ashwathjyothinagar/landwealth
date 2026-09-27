import React from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Alert, Chip, LinearProgress, Link, Paper, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { apiClient } from '../../api/client';
import { formatIndianRupee } from '../../utils/currencyFormatter';
import { DashboardSummary, DashboardFigures } from './DashboardSummary';
import { PortfolioTable, PortfolioRow } from './PortfolioTable';
import { ReminderNotices } from '../reminders/ReminderNotices';

interface ReminderRow {
  id: string;
  title: string;
  dueDate: string;
  status: string;
  priority: string;
}

interface RecentTransaction {
  id: string;
  transactionDate: string;
  transactionType: string;
  description: string;
  amount: number;
  status: string;
}

interface DashboardResponse extends DashboardFigures {
  portfolio: PortfolioRow[];
  upcomingReminders: ReminderRow[];
  recentTransactions: RecentTransaction[];
}

export const DashboardPage: React.FC = () => {
  const query = useQuery({
    queryKey: ['dashboard'],
    queryFn: async () => (await apiClient.get<DashboardResponse>('/api/dashboard')).data,
  });

  if (query.isLoading) return <LinearProgress />;
  if (query.isError || !query.data) return <Alert severity="error">The dashboard could not be loaded.</Alert>;

  return (
    <>
      <Typography variant="h4" gutterBottom>Portfolio</Typography>
      <Chip label="A property is not a bank account" color="success" sx={{ mb: 2 }} />
      <DashboardSummary figures={query.data} />
      <PortfolioTable rows={query.data.portfolio} />
      <Typography variant="h6" sx={{ mt: 3 }}>This month</Typography>
      <Paper>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Date</TableCell>
              <TableCell>Type</TableCell>
              <TableCell>Description</TableCell>
              <TableCell align="right">Amount</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {query.data.recentTransactions.length === 0 && (
              <TableRow><TableCell colSpan={4}>No transactions yet.</TableCell></TableRow>
            )}
            {query.data.recentTransactions.map((transaction) => (
              <TableRow key={transaction.id} hover>
                <TableCell>{transaction.transactionDate}</TableCell>
                <TableCell>{transaction.transactionType}</TableCell>
                <TableCell>
                  <Link component={RouterLink} to="/transactions">{transaction.description}</Link>
                </TableCell>
                <TableCell align="right">{formatIndianRupee(transaction.amount)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </Paper>
      <Typography variant="h6" sx={{ mt: 3 }}>Upcoming reminders</Typography>
      <ReminderNotices reminders={query.data.upcomingReminders} />
    </>
  );
};
