import React from 'react';
import { Paper, Table, TableBody, TableCell, TableHead, TableRow, Typography } from '@mui/material';
import { formatIndianRupee } from '../../utils/currencyFormatter';

export interface ProfitabilityRow {
  propertyId: string;
  name: string;
  acquisitionCost: number;
  improvements: number;
  costBasis: number;
  operatingIncome: number;
  operatingExpenses: number;
  unrealizedGain: number | null;
}

export const ProfitabilityTable: React.FC<{
  rows: ProfitabilityRow[];
  totalCostBasis: number;
  totalOperatingIncome: number;
  totalOperatingExpenses: number;
  totalUnrealizedGain: number;
}> = ({ rows, totalCostBasis, totalOperatingIncome, totalOperatingExpenses, totalUnrealizedGain }) => (
  <Paper sx={{ p: 2, mt: 2 }}>
    <Typography variant="h6">Property investment</Typography>
    <Table size="small">
      <TableHead>
        <TableRow>
          <TableCell>Property</TableCell>
          <TableCell align="right">Acquisition</TableCell>
          <TableCell align="right">Improvements</TableCell>
          <TableCell align="right">Cost basis</TableCell>
          <TableCell align="right">Income</TableCell>
          <TableCell align="right">Maintenance</TableCell>
          <TableCell align="right">Unrealized gain</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {rows.length === 0 && <TableRow><TableCell colSpan={7}>No properties yet.</TableCell></TableRow>}
        {rows.map((row) => (
          <TableRow key={row.propertyId}>
            <TableCell>{row.name}</TableCell>
            <TableCell align="right">{formatIndianRupee(row.acquisitionCost)}</TableCell>
            <TableCell align="right">{formatIndianRupee(row.improvements)}</TableCell>
            <TableCell align="right">{formatIndianRupee(row.costBasis)}</TableCell>
            <TableCell align="right">{formatIndianRupee(row.operatingIncome)}</TableCell>
            <TableCell align="right">{formatIndianRupee(row.operatingExpenses)}</TableCell>
            <TableCell align="right">{row.unrealizedGain == null ? '—' : formatIndianRupee(row.unrealizedGain)}</TableCell>
          </TableRow>
        ))}
        <TableRow>
          <TableCell>Total</TableCell>
          <TableCell />
          <TableCell />
          <TableCell align="right" data-testid="profit-basis">{formatIndianRupee(totalCostBasis)}</TableCell>
          <TableCell align="right">{formatIndianRupee(totalOperatingIncome)}</TableCell>
          <TableCell align="right">{formatIndianRupee(totalOperatingExpenses)}</TableCell>
          <TableCell align="right">{formatIndianRupee(totalUnrealizedGain)}</TableCell>
        </TableRow>
      </TableBody>
    </Table>
  </Paper>
);
