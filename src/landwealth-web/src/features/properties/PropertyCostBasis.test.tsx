import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { PropertyCostBasis } from './PropertyCostBasis';

const statement = {
  costBasis: 225000,
  latestValuation: null,
  unrealizedGain: null,
  activeExtentAcres: 3,
  allocatedCostBasis: 75000,
  realizedGain: 20000,
  acquisitionCost: 200000,
  improvements: 25000,
  maintenance: 8000,
};

describe('PropertyCostBasis', () => {
  it('shows the server breakdown and asks the server to estimate a sale', () => {
    const onEstimateSale = vi.fn();
    render(<PropertyCostBasis statement={statement} onEstimateSale={onEstimateSale} estimating={false} />);

    expect(screen.getByText('Acquisition ₹2,00,000.00')).toBeInTheDocument();
    expect(screen.getByText('Improvements ₹25,000.00')).toBeInTheDocument();
    expect(screen.getByTestId('cost-basis')).toHaveTextContent('₹2,25,000.00');
    expect(screen.getByText('Maintenance ₹8,000.00')).toBeInTheDocument();
    expect(screen.getByTestId('sale-result')).toHaveTextContent('₹75,000.00');
    expect(screen.getByTestId('sale-result')).toHaveTextContent('₹20,000.00');

    fireEvent.change(screen.getByLabelText('Extent sold'), { target: { value: '40' } });
    fireEvent.change(screen.getByLabelText('Gross proceeds'), { target: { value: '100000' } });
    fireEvent.click(screen.getByRole('button', { name: 'Estimate gain' }));
    expect(onEstimateSale).toHaveBeenCalledWith({
      extentSold: '40',
      extentUnit: 'Acres',
      grossProceeds: '100000',
      sellingExpenses: '0',
    });
  });
});
