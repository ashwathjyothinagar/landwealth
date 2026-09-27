export const transactionTypes = [
  { value: 'Income', label: 'Income' },
  { value: 'Expense', label: 'Expense' },
  { value: 'Transfer', label: 'Transfer' },
  { value: 'PropertyPurchase', label: 'Property purchase' },
  { value: 'PropertyExpense', label: 'Property expense' },
  { value: 'PropertyIncome', label: 'Property income' },
  { value: 'PropertySale', label: 'Property sale' },
  { value: 'Investment', label: 'Investment' },
  { value: 'LiabilityPayment', label: 'Liability payment' },
] as const;

export function transactionTypeLabel(value: string) {
  return transactionTypes.find((type) => type.value === value)?.label ?? value;
}

export function needsProperty(type: string) {
  return type.startsWith('Property');
}

export function needsCounterAccount(type: string) {
  return type === 'Transfer' || type === 'Investment' || type === 'LiabilityPayment';
}

export function categoryTypesFor(type: string) {
  switch (type) {
    case 'Income':
    case 'PropertyIncome':
    case 'PropertySale':
      return ['Income'];
    case 'Expense':
      return ['Expense'];
    case 'PropertyPurchase':
      return ['CapEx_Acquisition'];
    case 'PropertyExpense':
      return ['CapEx_Improvement', 'OpEx_Maintenance'];
    default:
      return [];
  }
}

export interface JournalLine {
  lineType: 'Debit' | 'Credit';
  amount: number;
  accountId?: string;
  propertyId?: string | null;
  categoryId?: string | null;
}

export function buildJournal(input: {
  type: string;
  amount: number;
  accountId: string;
  counterAccountId?: string;
  propertyId?: string;
  categoryId?: string;
}): JournalLine[] {
  const { amount } = input;
  const propertyId = input.propertyId || null;
  const categoryId = input.categoryId || null;
  if (needsCounterAccount(input.type)) {
    return [
      { lineType: 'Debit', amount, accountId: input.counterAccountId },
      { lineType: 'Credit', amount, accountId: input.accountId },
    ];
  }
  if (input.type === 'Expense' || input.type === 'PropertyPurchase' || input.type === 'PropertyExpense') {
    return [
      { lineType: 'Debit', amount, propertyId, categoryId },
      { lineType: 'Credit', amount, accountId: input.accountId },
    ];
  }
  return [
    { lineType: 'Debit', amount, accountId: input.accountId },
    { lineType: 'Credit', amount, propertyId, categoryId },
  ];
}

export function accountFieldLabels(type: string) {
  switch (type) {
    case 'Transfer':
      return { account: 'From account', counter: 'To account' };
    case 'Investment':
      return { account: 'Funded from', counter: 'Investment account' };
    case 'LiabilityPayment':
      return { account: 'Paid from', counter: 'Credit card' };
    case 'Income':
    case 'PropertyIncome':
    case 'PropertySale':
      return { account: 'Received in', counter: '' };
    default:
      return { account: 'Paid from', counter: '' };
  }
}
