import React from 'react';
import { Box, Typography } from '@mui/material';

export const documentTypes = [
  { value: 'SaleDeed', label: 'Sale deed' },
  { value: 'AgreementOfSale', label: 'Agreement of sale' },
  { value: 'EncumbranceCertificate_EC', label: 'Encumbrance certificate' },
  { value: 'RTC_Pahani', label: 'RTC / Pahani' },
  { value: 'MutationRegister', label: 'Mutation register' },
  { value: 'SurveySketch_Tippani_Akarband', label: 'Survey sketch' },
  { value: 'TaxReceipt', label: 'Tax receipt' },
  { value: 'KhataCertificate', label: 'Khata certificate' },
  { value: 'LegalOpinion', label: 'Legal opinion' },
  { value: 'CourtOrder', label: 'Court order' },
  { value: 'RegistrationReceipt', label: 'Registration receipt' },
  { value: 'SitePhoto', label: 'Site photo' },
  { value: 'Other', label: 'Other' },
] as const;

export function documentTypeLabel(value: string) {
  return documentTypes.find((type) => type.value === value)?.label ?? value;
}

export const DocumentDropZone: React.FC<{ fileName: string | null; onFile: (file: File) => void }> = ({ fileName, onFile }) => {
  const take = (list: FileList | null) => {
    const file = list?.[0];
    if (file) onFile(file);
  };

  return (
    <Box
      data-testid="document-drop"
      onDragOver={(event) => event.preventDefault()}
      onDrop={(event) => {
        event.preventDefault();
        take(event.dataTransfer.files);
      }}
      sx={{ border: '1px dashed', borderColor: 'divider', borderRadius: 1, p: 2, mb: 1 }}
    >
      <Typography>{fileName ? fileName : 'Drop a PDF, PNG, or JPEG here, or choose a file.'}</Typography>
      <input
        aria-label="Choose document"
        type="file"
        accept="application/pdf,image/png,image/jpeg,image/webp"
        onChange={(event) => take(event.target.files)}
      />
    </Box>
  );
};
