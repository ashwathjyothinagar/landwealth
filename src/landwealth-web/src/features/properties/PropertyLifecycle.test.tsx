import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { PropertyLifecycle } from './PropertyLifecycle';

describe('PropertyLifecycle', () => {
  it('offers only the next legal statuses', () => {
    const onTransition = vi.fn();
    render(
      <PropertyLifecycle status="Planned" nextStatuses={['Purchased', 'Archived']} onTransition={onTransition} />
    );

    expect(screen.getByTestId('property-status')).toHaveTextContent('Planned');
    expect(screen.getByRole('button', { name: 'Move to Purchased' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Move to Archived' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Move to Held' })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Move to Purchased' }));
    expect(onTransition).toHaveBeenCalledWith('Purchased');
  });
});
