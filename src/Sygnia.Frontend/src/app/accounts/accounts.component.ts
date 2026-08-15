import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AccountDto, AccountService } from '../services/account.service';

@Component({
  selector: 'app-accounts',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './accounts.component.html',
  styleUrl: './accounts.component.scss',
})
export class AccountsComponent {
  private readonly fb = inject(FormBuilder);
  private readonly accountService = inject(AccountService);

  readonly form = this.fb.nonNullable.group({
    accountId: ['', Validators.required],
    accountName: ['', Validators.required],
    contactPerson: ['', Validators.required],
    currency: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    createdBy: ['', Validators.required],
  });

  createdAccount: AccountDto | null = null;
  errorMessage: string | null = null;

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.createdAccount = null;
    this.errorMessage = null;

    this.accountService.createAccount(this.form.getRawValue()).subscribe({
      next: account => {
        this.createdAccount = account;
      },
      error: (err: { message?: string }) => {
        this.errorMessage = err?.message ?? 'An unexpected error occurred.';
      },
    });
  }
}
