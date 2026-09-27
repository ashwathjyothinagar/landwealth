import { describe, expect, it, vi } from 'vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { DocumentDropZone } from './DocumentDropZone';

describe('DocumentDropZone', () => {
  it('accepts a file dropped onto the page', () => {
    const onFile = vi.fn();
    render(<DocumentDropZone fileName={null} onFile={onFile} />);
    const file = new File(['%PDF-1.4'], 'sale-deed.pdf', { type: 'application/pdf' });
    fireEvent.drop(screen.getByTestId('document-drop'), {
      dataTransfer: { files: [file] },
    });
    expect(onFile).toHaveBeenCalledWith(file);
  });
});
