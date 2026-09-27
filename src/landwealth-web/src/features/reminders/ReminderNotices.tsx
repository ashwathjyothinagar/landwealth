import React from 'react';
import { Link as RouterLink } from 'react-router-dom';
import { Chip, Typography } from '@mui/material';

export interface ReminderNotice {
  id: string;
  title: string;
  dueDate: string;
  status: string;
  priority: string;
}

export const ReminderNotices: React.FC<{ reminders: ReminderNotice[] }> = ({ reminders }) => {
  if (reminders.length === 0) {
    return <Typography color="text.secondary">Nothing due in the next 30 days.</Typography>;
  }

  return (
    <>
      {reminders.map((reminder) => (
        <Typography key={reminder.id} component={RouterLink} to="/reminders" sx={{ display: 'block', mb: 1 }}>
          {reminder.dueDate}: {reminder.title}{' '}
          <Chip size="small" label={reminder.status} color={reminder.status === 'Overdue' ? 'error' : 'default'} data-testid={`reminder-${reminder.status}`} />
          {' '}
          <Chip size="small" label={reminder.priority} />
        </Typography>
      ))}
    </>
  );
};
