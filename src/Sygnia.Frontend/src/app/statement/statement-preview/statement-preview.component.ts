import { CommonModule } from '@angular/common';
import { Component, inject, input } from '@angular/core';
import { StatementLineDto } from '../../services/movement.service';
import { PdfExportService } from '../../services/pdf-export.service';

@Component({
  selector: 'app-statement-preview',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './statement-preview.component.html',
  styleUrl: './statement-preview.component.scss',
})
export class StatementPreviewComponent {
  private readonly pdfExportService = inject(PdfExportService);

  readonly lines = input<StatementLineDto[]>([]);

  /**
   * When set, overrides the default "export exactly the rows currently shown" behaviour —
   * used by the paginated statement view, where `lines()` is only the visible page and a
   * "Download PDF" click must instead export the whole account statement, not one page of it.
   */
  readonly onDownload = input<(() => void) | null>(null);

  downloadPdf(): void {
    const override = this.onDownload();
    if (override) {
      override();
      return;
    }
    this.pdfExportService.exportStatement(this.lines());
  }
}
