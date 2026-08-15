import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { GetStatementPageInput, MovementService, StatementPageDto } from '../services/movement.service';
import { StatementPreviewComponent } from './statement-preview/statement-preview.component';

const PAGE_SIZE = 25;

@Component({
  selector: 'app-statement',
  standalone: true,
  imports: [ReactiveFormsModule, StatementPreviewComponent],
  templateUrl: './statement.component.html',
  styleUrl: './statement.component.scss',
})
export class StatementComponent {
  private readonly fb = inject(FormBuilder);
  private readonly movementService = inject(MovementService);

  readonly pageSize = PAGE_SIZE;

  readonly form = this.fb.nonNullable.group({
    accountId: ['', Validators.required],
    from: [''],
    to: [''],
  });

  readonly currentPage = signal<StatementPageDto | null>(null);
  readonly pageNumber = signal(1);
  errorMessage: string | null = null;

  search(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.pageNumber.set(1);
    this.loadPage(1);
  }

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
