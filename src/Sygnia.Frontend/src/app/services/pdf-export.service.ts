import { Inject, Injectable, InjectionToken, inject } from '@angular/core';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import { StatementLineDto } from './movement.service';
import { ToastService } from '../shared/toast/toast.service';

/** Minimal surface PdfExportService needs from a jsPDF instance — lets tests substitute a fake. */
export interface PdfDocument {
  setFontSize(size: number): unknown;
  text(text: string, x: number, y: number): unknown;
  save(filename?: string): unknown;
}

/** Factory for creating a PdfDocument. Overridable in tests to avoid relying on jsPDF's internal mixin wiring. */
export const PDF_DOCUMENT_FACTORY = new InjectionToken<() => PdfDocument>('PDF_DOCUMENT_FACTORY', {
  providedIn: 'root',
  factory: () => () => new jsPDF() as unknown as PdfDocument,
});

/**
 * Renders a StatementLineDto[] as a simple PDF table and triggers a browser download.
 *
 * Takes StatementLineDto (not MovementDto[]) so the caller doesn't need to unwrap the
 * movement first; runningTotal is intentionally omitted from the table since
 * getStatementPage always returns it as null (see StatementLineDto doc comment in
 * movement.service.ts) — a running total isn't well-defined for an isolated page.
 */
@Injectable({ providedIn: 'root' })
export class PdfExportService {
  private readonly toastService = inject(ToastService);

  constructor(@Inject(PDF_DOCUMENT_FACTORY) private readonly createDoc: () => PdfDocument) {}

  exportStatement(lines: StatementLineDto[]): void {
    this.toastService.show('Preparing PDF for download…');

    // Deferred to a macrotask so the toast actually paints before autoTable's synchronous
    // table-building work (which can be substantial for a full streamed statement) blocks the
    // main thread — calling it in the same tick as show() would queue the DOM update but never
    // let the browser paint it first.
    setTimeout(() => {
      const doc = this.createDoc();

      doc.setFontSize(14);
      doc.text('Statement', 14, 15);

      const rows = lines.map(line => {
        const movement = line.movement;
        return [
          movement?.occurredAt ? movement.occurredAt.toISOString().slice(0, 10) : '',
          movement?.externalRef ?? '',
          movement?.currency ?? '',
          movement?.amount ?? '',
          movement?.narration ?? '',
        ];
      });

      autoTable(doc as unknown as jsPDF, {
        startY: 20,
        head: [['Date', 'External Ref', 'Currency', 'Amount', 'Narration']],
        body: rows,
      });

      doc.save('statement.pdf');
      this.toastService.show('PDF downloaded.', 2000);
    }, 0);
  }
}
