import React from 'react';

export interface ValuationPoint {
  date: string;
  estimatedValue: number;
  valuationSource: string;
}

export const valuationSources = [
  { value: 'GovernmentGuidanceValue', label: 'Guidance value' },
  { value: 'PrivateAppraiser', label: 'Private appraiser' },
  { value: 'BankValuation', label: 'Bank valuation' },
  { value: 'LocalMarketSurvey', label: 'Local market survey' },
  { value: 'OwnerEstimate', label: 'Owner estimate' },
] as const;

export function valuationSourceLabel(source: string) {
  return valuationSources.find((item) => item.value === source)?.label ?? source;
}

export function isGuidanceValue(source: string) {
  return source === 'GovernmentGuidanceValue';
}

export const ValuationChart: React.FC<{ points: ValuationPoint[] }> = ({ points }) => {
  const width = 440;
  const height = 160;
  const pad = 20;
  const guidance = points.filter((point) => isGuidanceValue(point.valuationSource));
  const market = points.filter((point) => !isGuidanceValue(point.valuationSource));
  const values = points.map((point) => point.estimatedValue);
  const min = values.length === 0 ? 0 : Math.min(...values);
  const max = values.length === 0 ? 1 : Math.max(...values);
  const span = max - min || 1;
  const xFor = (point: ValuationPoint) => {
    const index = points.indexOf(point);
    if (points.length <= 1) return width / 2;
    return pad + (index / (points.length - 1)) * (width - pad * 2);
  };
  const yFor = (value: number) => height - pad - ((value - min) / span) * (height - pad * 2);
  const pathFor = (series: ValuationPoint[]) => series.map((point, index) =>
    `${index === 0 ? 'M' : 'L'} ${xFor(point)} ${yFor(point.estimatedValue)}`).join(' ');

  if (points.length === 0) {
    return <p>No valuations recorded yet.</p>;
  }

  return (
    <figure>
      <svg data-testid="valuation-chart" width="100%" viewBox={`0 0 ${width} ${height}`} role="img" aria-label="Valuation history">
        {guidance.length > 0 && <path data-testid="guidance-series" d={pathFor(guidance)} fill="none" stroke="#8d6e63" strokeWidth="2" />}
        {market.length > 0 && <path data-testid="market-series" d={pathFor(market)} fill="none" stroke="#2e7d32" strokeWidth="2" />}
        {points.map((point) => (
          <circle key={`${point.date}-${point.valuationSource}`} cx={xFor(point)} cy={yFor(point.estimatedValue)} r="3" fill={isGuidanceValue(point.valuationSource) ? '#8d6e63' : '#2e7d32'} />
        ))}
      </svg>
      <figcaption>
        {guidance.length > 0 && <span>Guidance</span>}
        {market.length > 0 && <span>Market</span>}
      </figcaption>
    </figure>
  );
};
