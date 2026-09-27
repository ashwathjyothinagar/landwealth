import React, { useState } from 'react';
import { Alert, Box, Button, MenuItem, TextField, Typography } from '@mui/material';
import { formatIndianRupee } from '../../utils/currencyFormatter';

export interface PropertyAccounting {
  costBasis: number;
  latestValuation: number | null;
  unrealizedGain: number | null;
  activeExtentAcres: number;
  allocatedCostBasis: number | null;
  realizedGain: number | null;
  acquisitionCost: number;
  improvements: number;
  maintenance: number;
}

const extentUnits = ['Acres', 'Guntas', 'Cents', 'SqFt', 'SqYards', 'SqMeters', 'Bigha', 'Hectares'];

export const PropertyCostBasis: React.FC<{
  statement: PropertyAccounting;
  onEstimateSale: (input: { extentSold: string; extentUnit: string; grossProceeds: string; sellingExpenses: string }) => void;
  estimating: boolean;
}> = ({ statement, onEstimateSale, estimating }) => {
  const [extentSold, setExtentSold] = useState('');
  const [extentUnit, setExtentUnit] = useState('Acres');
  const [grossProceeds, setGrossProceeds] = useState('');
  const [sellingExpenses, setSellingExpenses] = useState('0');

  return (
    <Box sx={{ my: 2 }}>
      <Typography variant="h6">Cost basis</Typography>
      <Typography>Acquisition {formatIndianRupee(statement.acquisitionCost)}</Typography>
      <Typography>Improvements {formatIndianRupee(statement.improvements)}</Typography>
      <Typography data-testid="cost-basis">Cost basis {formatIndianRupee(statement.costBasis)}</Typography>
      <Typography>Maintenance {formatIndianRupee(statement.maintenance)}</Typography>
      <Typography color="text.secondary" variant="body2">
        Maintenance stays out of the cost basis. Active extent {statement.activeExtentAcres} acres.
      </Typography>
      {statement.latestValuation != null && (
        <Typography>Valuation {formatIndianRupee(statement.latestValuation)} · Unrealized {formatIndianRupee(statement.unrealizedGain ?? 0)}</Typography>
      )}
      <Typography variant="subtitle1" sx={{ mt: 2 }}>Sale estimate</Typography>
      <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap', my: 1 }}>
        <TextField label="Extent sold" value={extentSold} onChange={(event) => setExtentSold(event.target.value)} />
        <TextField select label="Unit" value={extentUnit} onChange={(event) => setExtentUnit(event.target.value)}>
          {extentUnits.map((unit) => <MenuItem key={unit} value={unit}>{unit}</MenuItem>)}
        </TextField>
        <TextField label="Gross proceeds" value={grossProceeds} onChange={(event) => setGrossProceeds(event.target.value)} />
        <TextField label="Selling expenses" value={sellingExpenses} onChange={(event) => setSellingExpenses(event.target.value)} />
        <Button
          variant="outlined"
          disabled={!extentSold || !grossProceeds || estimating}
          onClick={() => onEstimateSale({ extentSold, extentUnit, grossProceeds, sellingExpenses })}
        >
          Estimate gain
        </Button>
      </Box>
      {statement.allocatedCostBasis != null && (
        <Alert severity="info" data-testid="sale-result">
          Allocated basis {formatIndianRupee(statement.allocatedCostBasis)}
          {statement.realizedGain != null && <> · Realized gain {formatIndianRupee(statement.realizedGain)}</>}
        </Alert>
      )}
    </Box>
  );
};
