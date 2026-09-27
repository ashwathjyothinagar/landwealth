import React from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { Paper, Table, TableBody, TableCell, TableHead, TableRow, Link } from '@mui/material';
import { formatIndianRupee } from '../../utils/currencyFormatter';

export interface PortfolioRow {
  propertyId: string;
  name: string;
  propertyType: string;
  status: string;
  activeExtentAcres: number;
  costBasis: number;
  estimatedValue: number | null;
  unrealizedGain: number | null;
}

const propertyTypeLabels: Record<string, string> = {
  AgriculturalLand: 'Agricultural land',
  ResidentialLand: 'Residential land',
  CommercialLand: 'Commercial land',
  House: 'House',
  Apartment: 'Apartment',
  IndustrialLand: 'Industrial land',
  Other: 'Other',
};

export const PortfolioTable: React.FC<{ rows: PortfolioRow[] }> = ({ rows }) => (
  <Paper sx={{ mt: 3 }}>
    <Table>
      <TableHead>
        <TableRow>
          <TableCell>Property</TableCell>
          <TableCell>Type</TableCell>
          <TableCell align="right">Extent</TableCell>
          <TableCell>Status</TableCell>
          <TableCell align="right">Cost basis</TableCell>
          <TableCell align="right">Valuation</TableCell>
          <TableCell align="right">Unrealized gain</TableCell>
        </TableRow>
      </TableHead>
      <TableBody>
        {rows.length === 0 && (
          <TableRow>
            <TableCell colSpan={7}>No properties yet. Add one from the Properties page.</TableCell>
          </TableRow>
        )}
        {rows.map((row) => (
          <TableRow key={row.propertyId} hover>
            <TableCell>
              <Link component={RouterLink} to={`/properties/${row.propertyId}`} data-testid="portfolio-row">
                {row.name}
              </Link>
            </TableCell>
            <TableCell>{propertyTypeLabels[row.propertyType] ?? row.propertyType}</TableCell>
            <TableCell align="right">{row.activeExtentAcres} acres</TableCell>
            <TableCell>{row.status}</TableCell>
            <TableCell align="right">{formatIndianRupee(row.costBasis)}</TableCell>
            <TableCell align="right">{row.estimatedValue == null ? '—' : formatIndianRupee(row.estimatedValue)}</TableCell>
            <TableCell align="right">{row.unrealizedGain == null ? '—' : formatIndianRupee(row.unrealizedGain)}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  </Paper>
);
