/**
 * Formats a numeric value into the Indian Rupee format.
 * Examples:
 *   formatIndianRupee(1000)       => "₹1,000.00"
 *   formatIndianRupee(100000)     => "₹1,00,000.00"
 *   formatIndianRupee(12000000)   => "₹1,20,00,000.00"
 */
function roundToPaise(amount: number): number {
  const sign = amount < 0 ? -1 : 1;
  const paise = Math.round((Math.abs(amount) + Number.EPSILON) * 100) / 100;
  return sign * paise;
}

export function formatIndianRupee(
  amount: number | null | undefined,
  includeDecimals: boolean = true
): string {
  if (amount === null || amount === undefined || isNaN(amount)) {
    return '₹0.00';
  }

  const isNegative = amount < 0;
  const absAmount = Math.abs(amount);

  const formatted = new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    minimumFractionDigits: includeDecimals ? 2 : 0,
    maximumFractionDigits: includeDecimals ? 2 : 0,
  }).format(roundToPaise(absAmount));

  return isNegative ? `-${formatted}` : formatted;
}

/**
 * Formats large amounts into concise Indian financial shorthand (Lakhs and Crores).
 * Examples:
 *   formatIndianShorthand(50000)     => "₹50,000"
 *   formatIndianShorthand(1500000)   => "₹15.00 L"
 *   formatIndianShorthand(25000000)  => "₹2.50 Cr"
 */
export function formatIndianShorthand(amount: number | null | undefined): string {
  if (amount === null || amount === undefined || isNaN(amount)) {
    return '₹0';
  }

  const isNegative = amount < 0;
  const abs = Math.abs(amount);

  let formatted = '';
  if (abs >= 10000000) {
    // 1 Crore = 10,000,000
    const cr = abs / 10000000;
    formatted = `₹${cr.toFixed(2)} Cr`;
  } else if (abs >= 100000) {
    // 1 Lakh = 100,000
    const lakh = abs / 100000;
    formatted = `₹${lakh.toFixed(2)} L`;
  } else {
    formatted = new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0,
    }).format(abs);
  }

  return isNegative ? `-${formatted}` : formatted;
}
