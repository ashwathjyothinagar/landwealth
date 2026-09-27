import React from 'react';
import { Box, Button, Chip, Typography } from '@mui/material';

const labels: Record<string, string> = {
  Planned: 'Planned',
  Purchased: 'Purchased',
  Held: 'Held',
  UnderDevelopment: 'Under development',
  ForSale: 'For sale',
  Sold: 'Sold',
  Archived: 'Archived',
};

export function statusLabel(status: string) {
  return labels[status] ?? status;
}

export const PropertyLifecycle: React.FC<{
  status: string;
  nextStatuses: string[];
  onTransition: (status: string) => void;
  disabled?: boolean;
}> = ({ status, nextStatuses, onTransition, disabled }) => (
  <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', flexWrap: 'wrap' }}>
    <Typography variant="body2" color="text.secondary">Status</Typography>
    <Chip data-testid="property-status" label={statusLabel(status)} color="primary" />
    {nextStatuses.map((next) => (
      <Button key={next} size="small" variant="outlined" disabled={disabled} onClick={() => onTransition(next)}>
        Move to {statusLabel(next)}
      </Button>
    ))}
    {nextStatuses.length === 0 && <Typography variant="body2" color="text.secondary">No further status changes.</Typography>}
  </Box>
);
