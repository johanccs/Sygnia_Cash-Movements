import { fakeAsync, flush, TestBed, tick } from '@angular/core/testing';
import jsPDF from 'jspdf';
import { PDF_DOCUMENT_FACTORY, PdfExportService } from './pdf-export.service';
import { StatementLineDto } from './movement.service';
import { ToastService } from '../shared/toast/toast.service';

describe('PdfExportService', () => {
  let service: PdfExportService;
  let saveSpy: jasmine.Spy;
  let toastService: ToastService;

  beforeEach(() => {
    // Use a real jsPDF instance (so jspdf-autotable's rendering has a genuine doc to work
    // with) but spy on its own `save` method (added per-instance by jsPDF's plugin mixin,
    // not on the prototype) to assert the download was triggered without a real file write.
    const realDoc = new jsPDF();
    saveSpy = spyOn(realDoc, 'save');

    TestBed.configureTestingModule({
      providers: [{ provide: PDF_DOCUMENT_FACTORY, useValue: () => realDoc }],
    });
    service = TestBed.inject(PdfExportService);
    toastService = TestBed.inject(ToastService);
  });

  const lines: StatementLineDto[] = [
    {
      movement: {
        accountId: 'ACC-001',
        externalRef: 'MOV-20240715-000123',
        currency: 'ZAR',
        amount: '100.00',
        occurredAt: new Date('2024-07-15T00:00:00Z'),
        narration: 'Deposit',
        refNr: 'abc-123',
        movedBy: 'teller1',
        movedDate: new Date('2024-07-15T00:00:00Z'),
      },
      runningTotal: null,
    },
  ];

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('triggers a PDF download by calling save once', fakeAsync(() => {
    service.exportStatement(lines);
    tick();

    expect(saveSpy).toHaveBeenCalledTimes(1);
    expect(saveSpy).toHaveBeenCalledWith('statement.pdf');

    flush(); // drain ToastService's own auto-dismiss timers
  }));

  it('does nothing harmful with an empty array', fakeAsync(() => {
    service.exportStatement([]);
    tick();

    expect(saveSpy).toHaveBeenCalledTimes(1);

    flush();
  }));

  it('shows a toast before the download starts and another once it completes', fakeAsync(() => {
    service.exportStatement(lines);

    expect(toastService.messages()).toEqual([jasmine.objectContaining({ text: 'Preparing PDF for download…' })]);

    tick();

    expect(toastService.messages().some(m => m.text === 'PDF downloaded.')).toBeTrue();

    flush();
  }));
});
