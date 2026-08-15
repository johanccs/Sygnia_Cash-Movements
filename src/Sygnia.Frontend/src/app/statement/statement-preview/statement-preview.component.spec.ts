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

  beforeEach(async () => {
    pdfExportServiceSpy = jasmine.createSpyObj('PdfExportService', ['exportStatement']);

    await TestBed.configureTestingModule({
      imports: [StatementPreviewComponent],
      providers: [{ provide: PdfExportService, useValue: pdfExportServiceSpy }],
    }).compileComponents();

    fixture = TestBed.createComponent(StatementPreviewComponent);
    fixture.componentRef.setInput('lines', lines);
    fixture.detectChanges();
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

  it('renders no rows for an empty input', () => {
    fixture.componentRef.setInput('lines', []);
    fixture.detectChanges();

    const rows = fixture.debugElement.queryAll(By.css('tbody tr'));
    expect(rows.length).toBe(0);
  });
});
