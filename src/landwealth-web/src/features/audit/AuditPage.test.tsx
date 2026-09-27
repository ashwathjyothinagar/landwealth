import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { AuditLogTable } from './AuditPage';

describe('AuditLogTable', () => {
  it('shows a reversal beside the original posting', () => {
    render(<AuditLogTable entries={[
      {
        id: 2,
        entityName: 'Transaction',
        entityId: 'rev-1',
        action: 'Reversal',
        timestamp: '2026-09-26T10:00:00Z',
        summary: 'Reverse the purchase',
      },
      {
        id: 1,
        entityName: 'Transaction',
        entityId: 'tx-1',
        action: 'Insert',
        timestamp: '2026-09-26T09:00:00Z',
        summary: 'Purchase',
      },
    ]} />);

    expect(screen.getByTestId('audit-Reversal')).toHaveTextContent('Reverse the purchase');
    expect(screen.getByTestId('audit-Insert')).toHaveTextContent('Purchase');
    expect(screen.getAllByText('Transaction')).toHaveLength(2);
  });
});
