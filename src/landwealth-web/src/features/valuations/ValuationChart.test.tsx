import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ValuationChart } from './ValuationChart';

describe('ValuationChart', () => {
  it('draws guidance and market as separate series', () => {
    render(<ValuationChart points={[
      { date: '2026-01-15', estimatedValue: 350000, valuationSource: 'LocalMarketSurvey' },
      { date: '2026-06-01', estimatedValue: 180000, valuationSource: 'GovernmentGuidanceValue' },
    ]} />);

    expect(screen.getByRole('img', { name: 'Valuation history' })).toBeInTheDocument();
    expect(screen.getByTestId('market-series')).toBeInTheDocument();
    expect(screen.getByTestId('guidance-series')).toBeInTheDocument();
    expect(screen.getByText('Guidance')).toBeInTheDocument();
    expect(screen.getByText('Market')).toBeInTheDocument();
  });
});
