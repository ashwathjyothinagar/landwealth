import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Alert, Button, LinearProgress, MenuItem, Paper, TextField, Typography } from '@mui/material';
import { apiClient } from '../../api/client';
import { formatIndianRupee } from '../../utils/currencyFormatter';
import { ValuationChart, ValuationPoint } from '../valuations/ValuationChart';
import { ProfitabilityTable, ProfitabilityRow } from './ProfitabilityTable';

interface BalanceLine {
  section: string;
  name: string;
  amount: number;
}

interface BalanceSheet {
  totalAssets: number;
  liabilities: number;
  netWorth: number;
  lines: BalanceLine[];
}

interface Statement {
  properties: ProfitabilityRow[];
  totalCostBasis: number;
  totalOperatingIncome: number;
  totalOperatingExpenses: number;
  totalUnrealizedGain: number;
}

export const ReportsPage: React.FC = () => {
  const today = new Date();
  const [from, setFrom] = useState(`${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-01`);
  const [to, setTo] = useState(today.toISOString().slice(0, 10));
  const [propertyId, setPropertyId] = useState('');
  const cashFlow = useQuery({
    queryKey: ['cash-flow', from, to],
    queryFn: async () => (await apiClient.get('/api/reports/cash-flow', { params: { from, to } })).data as { inflows: number; outflows: number; net: number },
  });
  const sheet = useQuery({
    queryKey: ['balance-sheet'],
    queryFn: async () => (await apiClient.get<BalanceSheet>('/api/reports/balance-sheet')).data,
  });
  const statement = useQuery({
    queryKey: ['profitability'],
    queryFn: async () => (await apiClient.get<Statement>('/api/reports/profitability')).data,
  });
  const properties = useQuery({
    queryKey: ['properties'],
    queryFn: async () => (await apiClient.get('/api/properties')).data as { id: string; name: string }[],
  });
  const timeline = useQuery({
    queryKey: ['valuation-timeline', propertyId],
    enabled: Boolean(propertyId),
    queryFn: async () => (await apiClient.get<Array<{ date: string; estimatedValue: number; source: string }>>(
      `/api/reports/properties/${propertyId}/valuations`
    )).data,
  });

  const download = async (path: string, filename: string) => {
    const response = await apiClient.get(path, { responseType: 'blob' });
    const url = URL.createObjectURL(response.data);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    anchor.click();
    URL.revokeObjectURL(url);
  };

  const points: ValuationPoint[] = (timeline.data ?? []).map((point) => ({
    date: point.date,
    estimatedValue: point.estimatedValue,
    valuationSource: point.source,
  }));

  return (
    <>
      <Typography variant="h4" gutterBottom>Reports</Typography>
      <TextField type="date" label="From" InputLabelProps={{ shrink: true }} value={from} onChange={(event) => setFrom(event.target.value)} sx={{ mr: 1 }} />
      <TextField type="date" label="To" InputLabelProps={{ shrink: true }} value={to} onChange={(event) => setTo(event.target.value)} sx={{ mr: 1 }} />
      <Button variant="outlined" onClick={() => window.print()} sx={{ mr: 1 }}>Print</Button>
      <Button variant="outlined" onClick={() => download('/api/reports/balance-sheet.csv', 'balance-sheet.csv')} sx={{ mr: 1 }}>Export balance sheet</Button>
      <Button variant="outlined" onClick={() => download('/api/reports/profitability.csv', 'profitability.csv')}>Export profitability</Button>
      {(cashFlow.isLoading || sheet.isLoading || statement.isLoading) && <LinearProgress sx={{ mt: 2 }} />}
      {(cashFlow.isError || sheet.isError || statement.isError) && <Alert severity="error" sx={{ mt: 2 }}>Reports could not be loaded.</Alert>}
      {cashFlow.data && (
        <Paper sx={{ p: 2, mt: 2 }}>
          <Typography variant="h6">Cash flow {from} to {to}</Typography>
          <Typography>Inflows {formatIndianRupee(cashFlow.data.inflows)}</Typography>
          <Typography>Outflows {formatIndianRupee(cashFlow.data.outflows)}</Typography>
          <Typography>Net {formatIndianRupee(cashFlow.data.net)}</Typography>
        </Paper>
      )}
      {sheet.data && (
        <Paper sx={{ p: 2, mt: 2 }}>
          <Typography variant="h6">Balance sheet</Typography>
          {sheet.data.lines.map((line) => (
            <Typography key={`${line.section}-${line.name}`}>{line.section}: {line.name} {formatIndianRupee(line.amount)}</Typography>
          ))}
          <Typography sx={{ mt: 1 }}>Total assets {formatIndianRupee(sheet.data.totalAssets)}</Typography>
          <Typography>Liabilities {formatIndianRupee(sheet.data.liabilities)}</Typography>
          <Typography variant="h6">Net worth {formatIndianRupee(sheet.data.netWorth)}</Typography>
        </Paper>
      )}
      {statement.data && (
        <ProfitabilityTable
          rows={statement.data.properties}
          totalCostBasis={statement.data.totalCostBasis}
          totalOperatingIncome={statement.data.totalOperatingIncome}
          totalOperatingExpenses={statement.data.totalOperatingExpenses}
          totalUnrealizedGain={statement.data.totalUnrealizedGain}
        />
      )}
      <Paper sx={{ p: 2, mt: 2 }}>
        <Typography variant="h6">Valuation history</Typography>
        <TextField select label="Property" value={propertyId} onChange={(event) => setPropertyId(event.target.value)} sx={{ minWidth: 240, my: 1 }}>
          {(properties.data ?? []).map((property) => <MenuItem key={property.id} value={property.id}>{property.name}</MenuItem>)}
        </TextField>
        {propertyId && <Button sx={{ ml: 1 }} onClick={() => download(`/api/reports/properties/${propertyId}/valuations.csv`, 'valuations.csv')}>Export valuations</Button>}
        {timeline.data && <ValuationChart points={points} />}
      </Paper>
    </>
  );
};
