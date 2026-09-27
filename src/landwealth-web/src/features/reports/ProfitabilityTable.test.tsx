import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ProfitabilityTable } from './ProfitabilityTable';

describe('ProfitabilityTable', () => {
  it('shows the statement totals from the server', () => {
    render(<ProfitabilityTable
      rows={[{
        propertyId: '1',
        name: 'Chitnahalli Farm',
        acquisitionCost: 200000,
        improvements: 25000,
        costBasis: 225000,
        operatingIncome: 15000,
        operatingExpenses: 8000,
        unrealizedGain: 125000,
      }]}
      totalCostBasis={225000}
      totalOperatingIncome={15000}
      totalOperatingExpenses={8000}
      totalUnrealizedGain={125000}
    />);

    expect(screen.getByText('Chitnahalli Farm')).toBeInTheDocument();
    expect(screen.getByTestId('profit-basis')).toHaveTextContent('₹2,25,000.00');
    expect(screen.getAllByText('₹15,000.00')).toHaveLength(2);
    expect(screen.getAllByText('₹8,000.00')).toHaveLength(2);
  });
});
