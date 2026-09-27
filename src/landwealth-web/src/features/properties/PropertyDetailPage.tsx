import React, { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Alert, Box, Button, LinearProgress, MenuItem, Paper, TextField, Typography } from '@mui/material';
import { AxiosError } from 'axios';
import { apiClient } from '../../api/client';
import { formatIndianRupee } from '../../utils/currencyFormatter';
import { PropertyLifecycle, statusLabel } from './PropertyLifecycle';
import { PropertyCostBasis, type PropertyAccounting } from './PropertyCostBasis';
import { ValuationHistory } from '../valuations/ValuationHistory';
import { DocumentLibrary } from '../documents/DocumentLibrary';

const extentUnits = ['Acres', 'Guntas', 'Cents', 'SqFt', 'SqYards', 'SqMeters', 'Bigha', 'Hectares'];

interface Parcel {
  id: string;
  surveyNumber: string;
  subdivisionNumber: string | null;
  extent: number;
  extentUnit: string;
  status: string;
  boundaryDescription: string | null;
  ownershipPercentage: number;
}

interface Owner {
  id: string;
  ownerName: string;
  ownershipPercentage: number;
  ownershipType: string;
  isActive: boolean;
  startDate: string;
  endDate: string | null;
}

interface PropertyDetails {
  id: string;
  name: string;
  status: string;
  state: string;
  village: string | null;
  primarySurveyNumber: string | null;
  purchaseDate: string | null;
  purchasePrice: number;
  notes: string | null;
  activeExtentAcres: number;
  activeOwnershipPercentage: number;
  nextStatuses: string[];
  parcels: Parcel[];
  owners: Owner[];
}

function problemMessage(error: unknown, fallback: string) {
  const detail = (error as AxiosError<{ detail?: string }>).response?.data?.detail;
  return detail || fallback;
}

function formatExtent(value: number, unit: string) {
  return `${new Intl.NumberFormat('en-IN', { maximumFractionDigits: 4 }).format(value)} ${unit}`;
}

export const PropertyDetailPage: React.FC = () => {
  const { id = '' } = useParams();
  const client = useQueryClient();
  const property = useQuery({
    queryKey: ['property', id],
    queryFn: async () => (await apiClient.get<PropertyDetails>(`/api/properties/${id}`)).data,
  });
  const [saleEstimate, setSaleEstimate] = useState<{
    extentSold: string;
    extentUnit: string;
    grossProceeds: string;
    sellingExpenses: string;
  } | null>(null);
  const accounting = useQuery({
    queryKey: ['property-accounting', id, saleEstimate],
    queryFn: async () => (await apiClient.get<PropertyAccounting>(`/api/properties/${id}/accounting`, {
      params: saleEstimate ?? undefined,
    })).data,
  });

  const [surveyNumber, setSurveyNumber] = useState('');
  const [subdivisionNumber, setSubdivisionNumber] = useState('');
  const [extent, setExtent] = useState('1');
  const [extentUnit, setExtentUnit] = useState('Acres');
  const [boundary, setBoundary] = useState('');
  const [ownerName, setOwnerName] = useState('');
  const [share, setShare] = useState('');
  const [ownershipType, setOwnershipType] = useState('TenancyInCommon');
  const [splitExtent, setSplitExtent] = useState('');
  const [remainderLabel, setRemainderLabel] = useState('1');
  const [splitLabel, setSplitLabel] = useState('2');
  const [selectedParcel, setSelectedParcel] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  const refresh = () => {
    client.invalidateQueries({ queryKey: ['property', id] });
    client.invalidateQueries({ queryKey: ['property-accounting', id] });
    client.invalidateQueries({ queryKey: ['properties'] });
    client.invalidateQueries({ queryKey: ['dashboard'] });
  };

  const fail = (error: unknown) => setFormError(problemMessage(error, 'The change was rejected.'));

  const transition = useMutation({
    mutationFn: (status: string) => apiClient.post(`/api/properties/${id}/status`, { status }),
    onSuccess: () => { setFormError(null); refresh(); },
    onError: fail,
  });
  const addParcel = useMutation({
    mutationFn: () => apiClient.post(`/api/properties/${id}/parcels`, {
      surveyNumber,
      subdivisionNumber: subdivisionNumber || null,
      extent: Number(extent),
      extentUnit,
      ownershipPercentage: 100,
      boundaryDescription: boundary || null,
    }),
    onSuccess: () => {
      setSurveyNumber('');
      setSubdivisionNumber('');
      setBoundary('');
      setFormError(null);
      refresh();
    },
    onError: fail,
  });
  const subdivide = useMutation({
    mutationFn: () => apiClient.post(`/api/properties/${id}/parcels/${selectedParcel}/subdivide`, {
      splitExtent: Number(splitExtent),
      remainderSubdivision: remainderLabel,
      splitSubdivision: splitLabel,
    }),
    onSuccess: () => { setSplitExtent(''); setFormError(null); refresh(); },
    onError: fail,
  });
  const addOwner = useMutation({
    mutationFn: () => apiClient.post(`/api/properties/${id}/owners`, {
      ownerName,
      ownershipPercentage: Number(share),
      ownershipType,
      startDate: new Date().toISOString().slice(0, 10),
    }),
    onSuccess: () => { setOwnerName(''); setShare(''); setFormError(null); refresh(); },
    onError: fail,
  });
  const transferOwner = useMutation({
    mutationFn: (ownerId: string) => apiClient.post(`/api/properties/${id}/owners/${ownerId}/transfer`, {
      endDate: new Date().toISOString().slice(0, 10),
    }),
    onSuccess: () => { setFormError(null); refresh(); },
    onError: fail,
  });

  if (property.isLoading) return <LinearProgress />;
  if (property.isError || !property.data) return <Alert severity="error">Property was not found.</Alert>;

  const data = property.data;
  const activeParcels = data.parcels.filter((parcel) => parcel.status === 'Active');
  const remainingShare = Math.max(0, 100 - data.activeOwnershipPercentage);

  return (
    <>
      <Typography variant="h4" gutterBottom>{data.name}</Typography>
      <Typography color="text.secondary" gutterBottom>
        {[data.village, data.state].filter(Boolean).join(', ')}
        {data.primarySurveyNumber ? ` · Survey ${data.primarySurveyNumber}` : ''}
        {` · Active extent ${formatExtent(data.activeExtentAcres, 'Acres')}`}
      </Typography>
      <PropertyLifecycle
        status={data.status}
        nextStatuses={data.nextStatuses}
        disabled={transition.isPending}
        onTransition={(status) => transition.mutate(status)}
      />
      {formError && <Alert severity="error" sx={{ my: 2 }}>{formError}</Alert>}

      <Typography variant="h6" sx={{ mt: 3 }}>Parcels</Typography>
      {data.parcels.length === 0 && <Typography color="text.secondary">Add the survey parcels that make up this holding.</Typography>}
      {data.parcels.map((parcel) => (
        <Paper key={parcel.id} sx={{ p: 1.5, my: 1 }}>
          <Typography>
            {parcel.surveyNumber}{parcel.subdivisionNumber ? `/${parcel.subdivisionNumber}` : ''} · {formatExtent(parcel.extent, parcel.extentUnit)} · {statusLabel(parcel.status)}
          </Typography>
          {parcel.boundaryDescription && <Typography variant="body2" color="text.secondary">{parcel.boundaryDescription}</Typography>}
        </Paper>
      ))}
      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: { md: 'repeat(4, 1fr)' }, my: 2 }}>
        <TextField label="Survey number" value={surveyNumber} onChange={(event) => setSurveyNumber(event.target.value)} />
        <TextField label="Subdivision" value={subdivisionNumber} onChange={(event) => setSubdivisionNumber(event.target.value)} />
        <TextField label="Extent" value={extent} onChange={(event) => setExtent(event.target.value)} />
        <TextField select label="Unit" value={extentUnit} onChange={(event) => setExtentUnit(event.target.value)}>
          {extentUnits.map((unit) => <MenuItem key={unit} value={unit}>{unit}</MenuItem>)}
        </TextField>
        <TextField label="Boundaries" value={boundary} onChange={(event) => setBoundary(event.target.value)} sx={{ gridColumn: { md: 'span 3' } }} />
        <Button variant="contained" disabled={!surveyNumber || addParcel.isPending} onClick={() => addParcel.mutate()}>Add parcel</Button>
      </Box>

      <Typography variant="h6">Split a parcel</Typography>
      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', my: 1 }}>
        <TextField select label="Active parcel" value={selectedParcel} onChange={(event) => setSelectedParcel(event.target.value)} sx={{ minWidth: 220 }}>
          {activeParcels.map((parcel) => (
            <MenuItem key={parcel.id} value={parcel.id}>
              {parcel.surveyNumber} · {formatExtent(parcel.extent, parcel.extentUnit)}
            </MenuItem>
          ))}
        </TextField>
        <TextField label="Extent to split off" value={splitExtent} onChange={(event) => setSplitExtent(event.target.value)} />
        <TextField label="Remainder hissa" value={remainderLabel} onChange={(event) => setRemainderLabel(event.target.value)} />
        <TextField label="New hissa" value={splitLabel} onChange={(event) => setSplitLabel(event.target.value)} />
        <Button variant="outlined" disabled={!selectedParcel || !splitExtent || subdivide.isPending} onClick={() => subdivide.mutate()}>Split</Button>
      </Box>

      <Typography variant="h6" sx={{ mt: 2 }}>Joint ownership</Typography>
      <Typography color="text.secondary" sx={{ mb: 1 }}>
        Active share {data.activeOwnershipPercentage}% · {remainingShare}% still unallocated. The total cannot exceed 100%.
      </Typography>
      {data.owners.map((owner) => (
        <Box key={owner.id} sx={{ display: 'flex', gap: 2, alignItems: 'center', mb: 1 }}>
          <Typography>
            {owner.ownerName} · {owner.ownershipPercentage}% · {owner.ownershipType} · {owner.isActive ? 'Active' : `Ended ${owner.endDate}`}
          </Typography>
          {owner.isActive && (
            <Button size="small" onClick={() => transferOwner.mutate(owner.id)} disabled={transferOwner.isPending}>End ownership</Button>
          )}
        </Box>
      ))}
      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', mt: 1 }}>
        <TextField label="Owner" value={ownerName} onChange={(event) => setOwnerName(event.target.value)} />
        <TextField label="Share %" value={share} onChange={(event) => setShare(event.target.value)} />
        <TextField select label="Type" value={ownershipType} onChange={(event) => setOwnershipType(event.target.value)}>
          {['Freehold', 'JointTenancy', 'TenancyInCommon', 'Leasehold'].map((type) => <MenuItem key={type} value={type}>{type}</MenuItem>)}
        </TextField>
        <Button variant="outlined" disabled={!ownerName || !share || addOwner.isPending} onClick={() => addOwner.mutate()}>Add owner</Button>
      </Box>
      <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 3 }}>
        Purchase price on record {formatIndianRupee(data.purchasePrice)}.
      </Typography>
      {accounting.data && (
        <PropertyCostBasis
          statement={accounting.data}
          estimating={accounting.isFetching}
          onEstimateSale={setSaleEstimate}
        />
      )}
      {accounting.isError && <Alert severity="error">The cost basis could not be loaded.</Alert>}
      <ValuationHistory propertyId={id} />
      <DocumentLibrary propertyId={id} parcels={data.parcels} />
    </>
  );
};
