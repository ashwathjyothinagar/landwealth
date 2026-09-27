import { describe, expect, it } from 'vitest';
import { accountTypeLabel, balanceCaption, requiresLastFour } from './accountLabels';

describe('account labels', () => {
  it('names the managed account kinds and requires a mask for banks and cards', () => {
    expect(accountTypeLabel('BankAccount')).toBe('Bank account');
    expect(accountTypeLabel('CashAccount')).toBe('Cash drawer');
    expect(accountTypeLabel('CreditCard')).toBe('Credit card');
    expect(requiresLastFour('BankAccount')).toBe(true);
    expect(requiresLastFour('CreditCard')).toBe(true);
    expect(requiresLastFour('CashAccount')).toBe(false);
    expect(balanceCaption('CreditCard')).toBe('Amount owed');
    expect(balanceCaption('BankAccount')).toBe('Balance');
  });
});
