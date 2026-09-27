import { describe, expect, it } from 'vitest';
import { buildJournal, categoryTypesFor } from './transactionJournal';

describe('transaction journal', () => {
  it('builds a balanced expense and a transfer between two accounts', () => {
    expect(buildJournal({
      type: 'Expense',
      amount: 1500,
      accountId: 'bank',
      categoryId: 'household',
    })).toEqual([
      { lineType: 'Debit', amount: 1500, propertyId: null, categoryId: 'household' },
      { lineType: 'Credit', amount: 1500, accountId: 'bank' },
    ]);

    expect(buildJournal({
      type: 'Transfer',
      amount: 1000,
      accountId: 'bank',
      counterAccountId: 'cash',
    })).toEqual([
      { lineType: 'Debit', amount: 1000, accountId: 'cash' },
      { lineType: 'Credit', amount: 1000, accountId: 'bank' },
    ]);

    expect(categoryTypesFor('PropertyExpense')).toEqual(['CapEx_Improvement', 'OpEx_Maintenance']);
    expect(categoryTypesFor('Transfer')).toEqual([]);
  });
});
