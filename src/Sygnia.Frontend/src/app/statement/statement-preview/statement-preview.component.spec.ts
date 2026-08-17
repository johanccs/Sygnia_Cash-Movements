import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { StatementPreviewComponent } from './statement-preview.component';
import { PdfExportService } from '../../services/pdf-export.service';
import { StatementLineDto } from '../../services/movement.service';

describe('StatementPreviewComponent', () => {
  let fixture: ComponentFixture<StatementPreviewComponent>;
  let pdfExportServiceSpy: jasmine.SpyObj<PdfExportService>;

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
    {
      movement: {
        accountId: 'ACC-001',
        externalRef: 'MOV-20240716-000124',
        currency: 'ZAR',
        amount: '-50.00',
        occurredAt: new Date('2024-07-16T00:00:00Z'),
        narration: 'Withdrawal',
        refNr: 'abc-124',
        movedBy: 'teller1',
        movedDate: new Date('2024-07-16T00:00:00Z'),
      },
      runningTotal: null,
    },
  ];

  // cdk-virtual-scroll-viewport computes its visible range asynchronously (ResizeObserver/rAF),
  // so every test that reads rendered rows must let the fixture settle before asserting.
  async function setLinesAndSettle(value: StatementLineDto[]): Promise<void> {
    fixture.componentRef.setInput('lines', value);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  beforeEach(async () => {
    pdfExportServiceSpy = jasmine.createSpyObj('PdfExportService', ['exportStatement']);

    await TestBed.configureTestingModule({
      imports: [StatementPreviewComponent],
      providers: [{ provide: PdfExportService, useValue: pdfExportServiceSpy }],
    }).compileComponents();

    fixture = TestBed.createComponent(StatementPreviewComponent);
    await setLinesAndSettle(lines);
  });

  it('renders one row per line', () => {
    const rows = fixture.debugElement.queryAll(By.css('tbody tr'));
    expect(rows.length).toBe(2);
  });

  it('renders the external ref, currency, amount and narration for each line', () => {
    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('MOV-20240715-000123');
    expect(text).toContain('ZAR');
    expect(text).toContain('100.00');
    expect(text).toContain('Deposit');
  });

  it('calls PdfExportService.exportStatement when Download PDF is clicked', () => {
    const button = fixture.debugElement.query(By.css('button.download-pdf'));
    button.nativeElement.click();

    expect(pdfExportServiceSpy.exportStatement).toHaveBeenCalledWith(lines);
  });

  it('renders no rows for an empty input', async () => {
    await setLinesAndSettle([]);

    const rows = fixture.debugElement.queryAll(By.css('tbody tr'));
    expect(rows.length).toBe(0);
  });

  it('renders a real running total for streamed lines and a blank placeholder for paged lines (null)', async () => {
    const streamed: StatementLineDto[] = [{ ...lines[0], runningTotal: '100.00' }];
    await setLinesAndSettle(streamed);

    const cells = fixture.debugElement.queryAll(By.css('tbody tr td'));
    expect(cells[cells.length - 1].nativeElement.textContent.trim()).toBe('100.00');

    await setLinesAndSettle([lines[0]]); // runningTotal: null

    const nullCells = fixture.debugElement.queryAll(By.css('tbody tr td'));
    expect(nullCells[nullCells.length - 1].nativeElement.textContent.trim()).toBe('—');
  });

  it('keeps the DOM row count bounded for a 50,000-row statement (virtual scroll)', async () => {
    const manyLines: StatementLineDto[] = Array.from({ length: 50_000 }, (_, i) => ({
      movement: {
        accountId: 'ACC-001',
        externalRef: `MOV-SEED-${i}`,
        currency: 'ZAR',
        amount: '100.00',
        occurredAt: new Date('2024-01-01T00:00:00Z'),
        narration: 'Seeded statement row',
        refNr: `ref-${i}`,
        movedBy: 'seed-script',
        movedDate: new Date('2024-01-01T00:00:00Z'),
      },
      runningTotal: '100.00',
    }));

    await setLinesAndSettle(manyLines);

    const rows = fixture.debugElement.queryAll(By.css('tbody tr'));
    // Only rows near the viewport should ever be in the DOM, regardless of how many are in
    // lines() — this is the whole point of virtualizing (see component doc comment). A fixed
    // upper bound well under 50,000 proves the table isn't rendering every row.
    expect(rows.length).toBeLessThan(100);
    expect(rows.length).toBeGreaterThan(0);
  });
});
