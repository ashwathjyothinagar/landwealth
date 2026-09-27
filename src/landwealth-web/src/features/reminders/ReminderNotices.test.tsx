import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { ReminderNotices } from './ReminderNotices';

describe('ReminderNotices', () => {
  it('marks an overdue reminder', () => {
    render(
      <MemoryRouter>
        <ReminderNotices reminders={[{
          id: '1',
          title: 'Pay property tax',
          dueDate: '2026-09-25',
          status: 'Overdue',
          priority: 'High',
        }]} />
      </MemoryRouter>
    );

    expect(screen.getByText(/Pay property tax/)).toBeInTheDocument();
    expect(screen.getByTestId('reminder-Overdue')).toHaveTextContent('Overdue');
  });
});
