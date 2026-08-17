import { CommonModule } from '@angular/common';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { Component, inject, input } from '@angular/core';
import { StatementLineDto } from '../../services/movement.service';
import { PdfExportService } from '../../services/pdf-export.service';
import { TooltipDirective } from '../../shared/directives/tooltip.directive';

@Component({
  selector: 'app-statement-preview',
  standalone: true,
  imports: [CommonModule, ScrollingModule, TooltipDirective],
  templateUrl: './statement-preview.component.html',
  styleUrl: './statement-preview.component.scss',
})
export class StatementPreviewComponent {
  private readonly pdfExportService = inject(PdfExportService);

  readonly lines = input<StatementLineDto[]>([]);

  /**
   * A streamed 50,000+ row statement rendered as plain `@for` rows crashes the tab: every row
   * becomes live DOM (50,000 × 6 cells ≈ 300k nodes) with no windowing. cdk-virtual-scroll-
   * viewport keeps only the rows near the viewport in the DOM regardless of how many are in
   * `lines()`. trackBy avoids re-creating rows that are merely shifting position.
   */
  trackByExternalRef = (_index: number, line: StatementLineDto): string =>
    line.movement?.externalRef ?? `${_index}`;

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
