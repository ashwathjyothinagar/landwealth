import React, { useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, Button, LinearProgress, MenuItem, Paper, Table, TableBody, TableCell, TableHead, TableRow, TextField, Typography } from '@mui/material';
import { apiClient } from '../../api/client';
import { formatIndianRupee } from '../../utils/currencyFormatter';

interface PropertyRow {
  id: string;
  name: string;
  propertyType: string;
  status: string;
  state: string;
  village: string | null;
  purchasePrice: number;
  activeExtentAcres: number;
}

export const PropertiesPage: React.FC = () => {
  const client = useQueryClient();
  const query = useQuery({
    queryKey: ['properties'],
    queryFn: async () => (await apiClient.get<PropertyRow[]>('/api/properties')).data,
  });
  const [name, setName] = useState('');
  const [state, setState] = useState('Karnataka');
  const [propertyType, setPropertyType] = useState('AgriculturalLand');
  const [village, setVillage] = useState('');
  const create = useMutation({
    mutationFn: () => apiClient.post('/api/properties', { name, propertyType, state, village: village || null }),
    onSuccess: () => {
      setName('');
      setVillage('');
      client.invalidateQueries({ queryKey: ['properties'] });
    },
  });

  return (
    <>
      <Typography variant="h4" gutterBottom>Properties</Typography>
      <Paper sx={{ p: 2, mb: 2, display: 'grid', gap: 2, gridTemplateColumns: { md: '2fr 1fr 1fr 1fr auto' } }}>
        <TextField label="Name" value={name} onChange={(event) => setName(event.target.value)} />
        <TextField select label="Type" value={propertyType} onChange={(event) => setPropertyType(event.target.value)}>
          {['AgriculturalLand', 'ResidentialLand', 'CommercialLand', 'House', 'Apartment', 'IndustrialLand', 'Other'].map((type) => (
            <MenuItem key={type} value={type}>{type}</MenuItem>
          ))}
        </TextField>
        <TextField label="Village" value={village} onChange={(event) => setVillage(event.target.value)} />
        <TextField label="State" value={state} onChange={(event) => setState(event.target.value)} />
        <Button variant="contained" disabled={!name || !state || create.isPending} onClick={() => create.mutate()}>Add</Button>
      </Paper>
      {create.isError && <Alert severity="error">The property could not be saved.</Alert>}
      {query.isLoading && <LinearProgress />}
      {query.isError && <Alert severity="error">Properties could not be loaded.</Alert>}
      {query.data && (
        <Paper>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>Name</TableCell>
                <TableCell>Type</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Place</TableCell>
                <TableCell align="right">Active acres</TableCell>
                <TableCell align="right">Purchase price</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {query.data.length === 0 && (
                <TableRow><TableCell colSpan={6}>No holdings yet.</TableCell></TableRow>
              )}
              {query.data.map((property) => (
                <TableRow key={property.id} hover component={RouterLink} to={`/properties/${property.id}`} sx={{ textDecoration: 'none' }}>
                  <TableCell>{property.name}</TableCell>
                  <TableCell>{property.propertyType}</TableCell>
                  <TableCell>{property.status}</TableCell>
                  <TableCell>{[property.village, property.state].filter(Boolean).join(', ')}</TableCell>
                  <TableCell align="right">{property.activeExtentAcres}</TableCell>
                  <TableCell align="right">{formatIndianRupee(property.purchasePrice)}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Paper>
      )}
    </>
  );
};
