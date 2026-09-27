import React, { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import { Alert, Box, Button, MenuItem, TextField, Typography } from '@mui/material';
import { apiClient } from '../../api/client';
import { DocumentDropZone, documentTypeLabel, documentTypes } from './DocumentDropZone';

interface DocumentRow {
  id: string;
  parcelId: string | null;
  documentType: string;
  documentNumber: string | null;
  issueDate: string | null;
  originalFileName: string;
  notes: string | null;
}

interface ParcelOption {
  id: string;
  surveyNumber: string;
}

function problemMessage(error: unknown, fallback: string) {
  const detail = (error as AxiosError<{ detail?: string }>).response?.data?.detail;
  return detail || fallback;
}

export const DocumentLibrary: React.FC<{ propertyId: string; parcels: ParcelOption[] }> = ({ propertyId, parcels }) => {
  const client = useQueryClient();
  const documents = useQuery({
    queryKey: ['documents', propertyId],
    queryFn: async () => (await apiClient.get<DocumentRow[]>(`/api/properties/${propertyId}/documents`)).data,
  });
  const [file, setFile] = useState<File | null>(null);
  const [documentType, setDocumentType] = useState('SaleDeed');
  const [documentNumber, setDocumentNumber] = useState('');
  const [issueDate, setIssueDate] = useState('');
  const [parcelId, setParcelId] = useState('');
  const [notes, setNotes] = useState('');
  const [formError, setFormError] = useState<string | null>(null);

  const upload = useMutation({
    mutationFn: async () => {
      const body = new FormData();
      body.append('file', file as File);
      body.append('documentType', documentType);
      if (documentNumber) body.append('documentNumber', documentNumber);
      if (issueDate) body.append('issueDate', issueDate);
      if (parcelId) body.append('parcelId', parcelId);
      if (notes) body.append('notes', notes);
      await apiClient.post(`/api/properties/${propertyId}/documents`, body, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
    },
    onSuccess: () => {
      setFile(null);
      setDocumentNumber('');
      setNotes('');
      setFormError(null);
      client.invalidateQueries({ queryKey: ['documents', propertyId] });
    },
    onError: (error) => setFormError(problemMessage(error, 'The document was rejected.')),
  });

  const download = async (row: DocumentRow) => {
    const response = await apiClient.get(`/api/documents/${row.id}/download`, { responseType: 'blob' });
    const url = URL.createObjectURL(response.data);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = row.originalFileName;
    anchor.click();
    URL.revokeObjectURL(url);
  };

  return (
    <Box sx={{ mt: 3 }}>
      <Typography variant="h6">Documents</Typography>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
        Files are stored under a private id. The page keeps the document type, number, date, and notes.
      </Typography>
      {documents.isError && <Alert severity="error">Documents could not be loaded.</Alert>}
      {(documents.data ?? []).length === 0 && documents.data && <Alert severity="info">No documents yet.</Alert>}
      {(documents.data ?? []).map((row) => (
        <Box key={row.id} sx={{ display: 'flex', gap: 2, alignItems: 'center', mb: 1 }}>
          <Typography>
            {documentTypeLabel(row.documentType)} · {row.originalFileName}
            {row.documentNumber ? ` · ${row.documentNumber}` : ''}
            {row.issueDate ? ` · ${row.issueDate}` : ''}
            {row.notes ? ` · ${row.notes}` : ''}
          </Typography>
          <Button size="small" onClick={() => download(row)}>Download</Button>
        </Box>
      ))}
      <DocumentDropZone fileName={file?.name ?? null} onFile={setFile} />
      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
        <TextField select label="Document type" value={documentType} onChange={(event) => setDocumentType(event.target.value)}>
          {documentTypes.map((type) => <MenuItem key={type.value} value={type.value}>{type.label}</MenuItem>)}
        </TextField>
        <TextField label="Document number" value={documentNumber} onChange={(event) => setDocumentNumber(event.target.value)} />
        <TextField type="date" label="Issue date" InputLabelProps={{ shrink: true }} value={issueDate} onChange={(event) => setIssueDate(event.target.value)} />
        <TextField select label="Parcel" value={parcelId} onChange={(event) => setParcelId(event.target.value)}>
          <MenuItem value="">Whole property</MenuItem>
          {parcels.map((parcel) => <MenuItem key={parcel.id} value={parcel.id}>{parcel.surveyNumber}</MenuItem>)}
        </TextField>
        <TextField label="Notes" value={notes} onChange={(event) => setNotes(event.target.value)} />
        <Button variant="outlined" disabled={!file || upload.isPending} onClick={() => upload.mutate()}>Upload</Button>
      </Box>
      {formError && <Alert severity="error" sx={{ mt: 1 }}>{formError}</Alert>}
    </Box>
  );
};
