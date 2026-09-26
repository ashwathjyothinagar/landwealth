import { describe, it, expect } from 'vitest';
import { formatIndianRupee, formatIndianShorthand } from './currencyFormatter';

describe('currencyFormatter', () => {
  describe('formatIndianRupee', () => {
    it('should format zero correctly', () => {
      expect(formatIndianRupee(0)).toContain('0.00');
    });

    it('should format thousands correctly', () => {
      const result = formatIndianRupee(50000);
      expect(result).toMatch(/50,000\.00/);
    });

    it('should format lakhs correctly with Indian grouping', () => {
      // 10 Lakhs = 10,00,000
      const result = formatIndianRupee(1000000);
      expect(result).toMatch(/10,00,000\.00/);
    });

    it('should format crores correctly with Indian grouping', () => {
      // 1.25 Crore = 1,25,00,000
      const result = formatIndianRupee(12500000);
      expect(result).toMatch(/1,25,00,000\.00/);
    });

    it('should format negative values with minus prefix', () => {
      const result = formatIndianRupee(-50000);
      expect(result).toMatch(/-.*50,000\.00/);
    });
  });

  describe('formatIndianShorthand', () => {
    it('should format amounts in Crores with Cr suffix', () => {
      expect(formatIndianShorthand(25000000)).toBe('₹2.50 Cr');
    });

    it('should format amounts in Lakhs with L suffix', () => {
      expect(formatIndianShorthand(1500000)).toBe('₹15.00 L');
    });

    it('should format small amounts as standard currency without decimals', () => {
      expect(formatIndianShorthand(45000)).toMatch(/45,000/);
    });
  });
});
