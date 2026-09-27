export const accountTypes = [
  { value: 'BankAccount', label: 'Bank account' },
  { value: 'CashAccount', label: 'Cash drawer' },
  { value: 'CreditCard', label: 'Credit card' },
  { value: 'OtherFinancialAccount', label: 'Other account' },
] as const;

export function accountTypeLabel(accountType: string) {
  return accountTypes.find((type) => type.value === accountType)?.label ?? accountType;
}

export function requiresLastFour(accountType: string) {
  return accountType === 'BankAccount' || accountType === 'CreditCard';
}

export function balanceCaption(accountType: string) {
  return accountType === 'CreditCard' ? 'Amount owed' : 'Balance';
}
