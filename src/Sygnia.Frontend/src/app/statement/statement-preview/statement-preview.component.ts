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

  downloadPdf(): void {
    this.pdfExportService.exportStatement(this.lines());
  }
}
