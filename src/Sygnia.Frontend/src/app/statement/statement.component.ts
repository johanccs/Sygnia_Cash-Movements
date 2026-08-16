import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  GetStatementPageInput,
  MovementService,
  StatementLineDto,
  StatementPageDto,
} from '../services/movement.service';
import { StatementPreviewComponent } from './statement-preview/statement-preview.component';
import { AccountDto, AccountService } from '../services/account.service';
import { PdfExportService } from '../services/pdf-export.service';

const PAGE_SIZE = 25;

@Component({
  selector: 'app-statement',
  standalone: true,
  imports: [ReactiveFormsModule, StatementPreviewComponent],
  templateUrl: './statement.component.html',
  styleUrl: './statement.component.scss',
})
export class StatementComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly movementService = inject(MovementService);
  private readonly accountService = inject(AccountService);
  private readonly pdfExportService = inject(PdfExportService);

  readonly pageSize = PAGE_SIZE;
  accounts: AccountDto[] = [];

  ngOnInit(): void {
    this.accountService.listAccounts().subscribe(accounts => {
      this.accounts = accounts;
    });
  }

  readonly form = this.fb.nonNullable.group({
    accountId: ['', Validators.required],
    from: [''],
    to: [''],
  });

  readonly currentPage = signal<StatementPageDto | null>(null);
  readonly pageNumber = signal(1);
  errorMessage: string | null = null;

  /**
   * The full, unpaginated statement, populated by streamStatement one row at a time as it
   * arrives over the wire — never assembled from a buffered array. Kept separate from
   * currentPage() so the two RPCs (paged vs. streamed) don't fight over the same view state.
   */
  readonly streamedLines = signal<StatementLineDto[]>([]);
  readonly isStreaming = signal(false);

  search(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.pageNumber.set(1);
    this.loadPage(1);
  }

  streamFullStatement(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { accountId, from, to } = this.form.getRawValue();
    this.errorMessage = null;
    this.streamedLines.set([]);
    this.currentPage.set(null);
    this.isStreaming.set(true);

    this.movementService
      .streamStatement({
        accountId,
        ...(from ? { from: new Date(from) } : {}),
        ...(to ? { to: new Date(to) } : {}),
      })
      .subscribe({
        // Appends and renders each row the moment it arrives, rather than waiting for the
        // stream to finish and setting the whole array once.
        next: line => this.streamedLines.update(lines => [...lines, line]),
        error: (err: { message?: string }) => {
          this.errorMessage = err?.message ?? 'An unexpected error occurred.';
          this.isStreaming.set(false);
        },
        complete: () => this.isStreaming.set(false),
      });
  }

  /**
   * "Download PDF" on the paginated view must export the whole statement, not the one page
   * on screen — streams it fresh (row by row, per the same streaming contract as
   * streamFullStatement) into a local array kept out of currentPage()/streamedLines(), so the
   * on-screen pagination is left untouched, then hands the complete array to PdfExportService.
   */
  exportFullStatementAsPdf = (): void => {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { accountId, from, to } = this.form.getRawValue();
    const lines: StatementLineDto[] = [];

    this.movementService
      .streamStatement({
        accountId,
        ...(from ? { from: new Date(from) } : {}),
        ...(to ? { to: new Date(to) } : {}),
      })
      .subscribe({
        next: line => lines.push(line),
        error: (err: { message?: string }) => {
          this.errorMessage = err?.message ?? 'An unexpected error occurred.';
        },
        complete: () => this.pdfExportService.exportStatement(lines),
      });
  };

  goToPage(pageNumber: number): void {
    if (pageNumber < 1 || pageNumber > this.totalPages()) {
      return;
    }
    this.pageNumber.set(pageNumber);
    this.loadPage(pageNumber);
  }

  totalPages(): number {
    const total = this.currentPage()?.totalCount ?? 0;
    return Math.max(1, Math.ceil(total / this.pageSize));
  }

  private loadPage(pageNumber: number): void {
    const { accountId, from, to } = this.form.getRawValue();

    this.errorMessage = null;
    this.streamedLines.set([]);

    const input: GetStatementPageInput = {
      accountId,
      pageNumber,
      pageSize: this.pageSize,
      ...(from ? { from: new Date(from) } : {}),
      ...(to ? { to: new Date(to) } : {}),
    };

    this.movementService.getStatementPage(input).subscribe({
      next: page => {
        this.currentPage.set(page);
      },
      error: (err: { message?: string }) => {
        this.errorMessage = err?.message ?? 'An unexpected error occurred.';
      },
    });
  }
}
