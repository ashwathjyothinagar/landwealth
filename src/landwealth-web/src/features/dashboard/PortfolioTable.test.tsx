import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { PortfolioTable } from './PortfolioTable';

describe('PortfolioTable', () => {
  it('opens the property from the portfolio row', () => {
    render(
      <MemoryRouter>
        <PortfolioTable rows={[{
          propertyId: 'farm-1',
          name: 'Chitnahalli Farm',
          propertyType: 'AgriculturalLand',
          status: 'Purchased',
          activeExtentAcres: 3,
          costBasis: 200000,
          estimatedValue: 350000,
          unrealizedGain: 150000,
        }]} />
      </MemoryRouter>
    );

    expect(screen.getByRole('link', { name: 'Chitnahalli Farm' })).toHaveAttribute('href', '/properties/farm-1');
    expect(screen.getByText('3 acres')).toBeInTheDocument();
    expect(screen.getByText('Agricultural land')).toBeInTheDocument();
  });
});
