import { Box, Card, CardContent, Grid, Typography } from '@mui/material';
import { formatIndianRupee } from '../../utils/currencyFormatter';

export interface DashboardFigures {
  liquidNetWorth: number;
  propertyInvestment: number;
  propertyValue: number;
  unrealizedGain: number;
  totalNetWorth: number;
  monthInflows: number;
  monthOutflows: number;
}

export const DashboardSummary: React.FC<{ figures: DashboardFigures }> = ({ figures }) => {
  const cards = [
    ['Liquid net worth', figures.liquidNetWorth, 'Bank, cash, and credit cards'],
    ['Property investment', figures.propertyInvestment, 'Capital cost basis'],
    ['Unrealized gain', figures.unrealizedGain, 'Valuation minus cost basis'],
    ['Total net worth', figures.totalNetWorth, 'Liquid + property value + other assets − liabilities'],
  ] as const;

  return (
    <Box>
      <Grid container spacing={2}>
        {cards.map(([label, amount, caption]) => (
          <Grid item xs={12} md={3} key={label}>
            <Card>
              <CardContent>
                <Typography variant="subtitle2" color="text.secondary">{label}</Typography>
                <Typography variant="h5" data-testid={label} sx={{ fontWeight: 700, color: amount < 0 ? 'error.main' : 'primary.main' }}>
                  {formatIndianRupee(amount)}
                </Typography>
                <Typography variant="caption" color="text.secondary">{caption}</Typography>
              </CardContent>
            </Card>
          </Grid>
        ))}
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary">Month inflows</Typography>
              <Typography variant="h6" data-testid="Month inflows" color="success.main">{formatIndianRupee(figures.monthInflows)}</Typography>
              <Typography variant="caption" color="text.secondary">Transfers between accounts are excluded.</Typography>
            </CardContent>
          </Card>
        </Grid>
        <Grid item xs={12} md={6}>
          <Card>
            <CardContent>
              <Typography variant="subtitle2" color="text.secondary">Month outflows</Typography>
              <Typography variant="h6" data-testid="Month outflows" color="error.main">{formatIndianRupee(figures.monthOutflows)}</Typography>
              <Typography variant="caption" color="text.secondary">Transfers between accounts are excluded.</Typography>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
};
