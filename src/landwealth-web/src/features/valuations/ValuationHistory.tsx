import React, { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { Alert, Box, Button, MenuItem, TextField, Typography } from '@mui/material';
import { apiClient } from '../../api/client';
import { formatIndianRupee } from '../../utils/currencyFormatter';
import { ValuationChart, valuationSourceLabel, valuationSources } from './ValuationChart';

interface ValuationRow {
  id: string;
  valuationDate: string;
  estimatedValue: number;
  valuationSource: string;
  notes: string | null;
}

function problemMessage(error: unknown, fallback: string) {
  const detail = (error as AxiosError<{ detail?: string }>).response?.data?.detail;
  return detail || fallback;
}

export const ValuationHistory: React.FC<{ propertyId: string }> = ({ propertyId }) => {
  const client = useQueryClient();
  const history = useQuery({
    queryKey: ['valuations', propertyId],
    queryFn: async () => (await apiClient.get<ValuationRow[]>(`/api/properties/${propertyId}/valuations`)).data,
  });
  const [date, setDate] = useState(new Date().toISOString().slice(0, 10));
  const [amount, setAmount] = useState('');
  const [source, setSource] = useState('LocalMarketSurvey');
  const [notes, setNotes] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  const add = useMutation({
    mutationFn: () => apiClient.post(`/api/properties/${propertyId}/valuations`, {
      valuationDate: date,
      estimatedValue: Number(amount),
      valuationSource: source,
      notes: notes || null,
    }),
    onSuccess: () => {
      setAmount('');
      setNotes('');
      setFormError(null);
      client.invalidateQueries({ queryKey: ['valuations', propertyId] });
      client.invalidateQueries({ queryKey: ['property-accounting', propertyId] });
      client.invalidateQueries({ queryKey: ['dashboard'] });
    },
    onError: (error) => setFormError(problemMessage(error, 'The valuation was rejected.')),
  });

  const points = (history.data ?? []).map((row) => ({
    date: row.valuationDate,
    estimatedValue: row.estimatedValue,
    valuationSource: row.valuationSource,
  }));

  return (
    <Box sx={{ mt: 3 }}>
      <Typography variant="h6">Valuations</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
        Guidance value and market estimates stay in the history. Unrealized gain follows the latest market estimate, or the guidance value when no market estimate exists. Recording a value does not move cash.
      </Typography>
      {history.isLoading && <Typography>Loading valuations…</Typography>}
      {history.isError && <Alert severity="error">Valuations could not be loaded.</Alert>}
      {history.data && <ValuationChart points={points} />}
      {(history.data ?? []).map((row) => (
        <Typography key={row.id}>
          {row.valuationDate} · {valuationSourceLabel(row.valuationSource)} · {formatIndianRupee(row.estimatedValue)}
          {row.notes ? ` · ${row.notes}` : ''}
        </Typography>
      ))}
      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mt: 1 }}>
        <TextField type="date" label="Date" InputLabelProps={{ shrink: true }} value={date} onChange={(event) => setDate(event.target.value)} />
        <TextField label="Estimated value" value={amount} onChange={(event) => setAmount(event.target.value)} />
        <TextField select label="Source" value={source} onChange={(event) => setSource(event.target.value)}>
          {valuationSources.map((item) => <MenuItem key={item.value} value={item.value}>{item.label}</MenuItem>)}
        </TextField>
        <TextField label="Notes" value={notes} onChange={(event) => setNotes(event.target.value)} />
        <Button variant="outlined" disabled={!amount || add.isPending} onClick={() => add.mutate()}>Record valuation</Button>
      </Box>
      {formError && <Alert severity="error" sx={{ mt: 1 }}>{formError}</Alert>}
    </Box>
  );
};
