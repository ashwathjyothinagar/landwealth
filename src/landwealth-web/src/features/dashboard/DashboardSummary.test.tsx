import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ThemeProvider } from '@mui/material';
import { theme } from '../../theme';
import { DashboardSummary } from './DashboardSummary';

describe('DashboardSummary', () => {
  it('renders backend totals with Indian grouping', () => {
    render(
      <ThemeProvider theme={theme}>
        <DashboardSummary
          figures={{
            liquidNetWorth: 93000,
            propertyInvestment: 200000,
            propertyValue: 350000,
            unrealizedGain: 150000,
            totalNetWorth: 1234567,
            monthInflows: 40000,
            monthOutflows: 5000,
          }}
        />
      </ThemeProvider>
    );

    expect(screen.getByTestId('Total net worth').textContent).toMatch(/12,34,567\.00/);
    expect(screen.getByTestId('Property investment').textContent).toMatch(/2,00,000\.00/);
  });
});
