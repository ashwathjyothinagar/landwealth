import React, { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { Alert, Button, LinearProgress, MenuItem, Paper, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from '@mui/material';
import { apiClient } from '../../api/client';

interface Reminder {
  id: string;
  title: string;
  description: string | null;
  dueDate: string;
  priority: string;
  status: string;
  propertyId: string | null;
}

const presets = [
  { label: 'Property tax', title: 'Pay property tax' },
  { label: 'Lease expiry', title: 'Lease expiry' },
  { label: 'Agricultural survey', title: 'Agricultural survey' },
];

function problemMessage(error: unknown, fallback: string) {
  const detail = (error as AxiosError<{ detail?: string }>).response?.data?.detail;
  return detail || fallback;
}

export const RemindersPage: React.FC = () => {
  const client = useQueryClient();
  const query = useQuery({
    queryKey: ['reminders'],
    queryFn: async () => (await apiClient.get<Reminder[]>('/api/reminders')).data,
  });
  const properties = useQuery({
    queryKey: ['properties'],
    queryFn: async () => (await apiClient.get('/api/properties')).data as { id: string; name: string }[],
  });
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [dueDate, setDueDate] = useState('');
  const [priority, setPriority] = useState('Medium');
  const [propertyId, setPropertyId] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  const refresh = () => {
    client.invalidateQueries({ queryKey: ['reminders'] });
    client.invalidateQueries({ queryKey: ['dashboard'] });
  };

  const create = useMutation({
    mutationFn: () => apiClient.post('/api/reminders', {
      title,
      description: description || null,
      dueDate,
      priority,
      propertyId: propertyId || null,
    }),
    onSuccess: () => {
      setTitle('');
      setDescription('');
      setFormError(null);
      refresh();
    },
    onError: (error) => setFormError(problemMessage(error, 'The reminder was rejected.')),
  });
  const complete = useMutation({
    mutationFn: (id: string) => apiClient.post(`/api/reminders/${id}/complete`, { completedDate: new Date().toISOString().slice(0, 10) }),
    onSuccess: refresh,
  });
  const dismiss = useMutation({
    mutationFn: (id: string) => apiClient.post(`/api/reminders/${id}/dismiss`),
    onSuccess: refresh,
  });

  const propertyName = (id: string | null) => properties.data?.find((property) => property.id === id)?.name;

  return (
    <>
      <Typography variant="h4" gutterBottom>Reminders</Typography>
      <Typography color="text.secondary" sx={{ mb: 2 }}>
        Property tax, lease expiry, and agricultural survey dates stay pending until you complete or dismiss them. A past due date is shown as overdue.
      </Typography>
      {query.isLoading && <LinearProgress />}
      {query.isError && <Alert severity="error">Reminders could not be loaded.</Alert>}
      {query.data && (
        <Paper sx={{ mb: 2 }}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Due</TableCell>
                <TableCell>Title</TableCell>
                <TableCell>Property</TableCell>
                <TableCell>Priority</TableCell>
                <TableCell>Status</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {query.data.length === 0 && <TableRow><TableCell colSpan={6}>No reminders yet.</TableCell></TableRow>}
              {query.data.map((reminder) => (
                <TableRow key={reminder.id}>
                  <TableCell>{reminder.dueDate}</TableCell>
                  <TableCell>
                    {reminder.title}
                    {reminder.description && <Typography variant="caption" display="block">{reminder.description}</Typography>}
                  </TableCell>
                  <TableCell>{propertyName(reminder.propertyId) ?? '—'}</TableCell>
                  <TableCell>{reminder.priority}</TableCell>
                  <TableCell>{reminder.status}</TableCell>
                  <TableCell>
                    {reminder.status !== 'Completed' && reminder.status !== 'Dismissed' && (
                      <>
                        <Button size="small" onClick={() => complete.mutate(reminder.id)}>Complete</Button>
                        <Button size="small" onClick={() => dismiss.mutate(reminder.id)}>Dismiss</Button>
                      </>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}
      <Paper sx={{ p: 2, display: 'grid', gap: 2, gridTemplateColumns: { md: 'repeat(3, 1fr)' } }}>
        {presets.map((preset) => (
          <Button key={preset.title} variant="outlined" onClick={() => setTitle(preset.title)}>{preset.label}</Button>
        ))}
        <TextField label="Title" value={title} onChange={(event) => setTitle(event.target.value)} />
        <TextField label="Description" value={description} onChange={(event) => setDescription(event.target.value)} />
        <TextField type="date" label="Due" InputLabelProps={{ shrink: true }} value={dueDate} onChange={(event) => setDueDate(event.target.value)} />
        <TextField select label="Priority" value={priority} onChange={(event) => setPriority(event.target.value)}>
          {['Low', 'Medium', 'High', 'Critical'].map((item) => <MenuItem key={item} value={item}>{item}</MenuItem>)}
        </TextField>
        <TextField select label="Property" value={propertyId} onChange={(event) => setPropertyId(event.target.value)}>
          <MenuItem value="">None</MenuItem>
          {(properties.data ?? []).map((property) => <MenuItem key={property.id} value={property.id}>{property.name}</MenuItem>)}
        </TextField>
        <Button variant="contained" disabled={!title || !dueDate || create.isPending} onClick={() => create.mutate()}>Add reminder</Button>
      </Paper>
      {formError && <Alert severity="error" sx={{ mt: 2 }}>{formError}</Alert>}
    </>
  );
};
