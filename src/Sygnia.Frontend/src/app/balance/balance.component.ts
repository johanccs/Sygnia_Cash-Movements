import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BalanceDto, MovementService } from '../services/movement.service';
import { AccountDto, AccountService } from '../services/account.service';

@Component({
  selector: 'app-balance',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './balance.component.html',
  styleUrl: './balance.component.scss',
})
export class BalanceComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly movementService = inject(MovementService);
  private readonly accountService = inject(AccountService);

  accounts: AccountDto[] = [];

  ngOnInit(): void {
    this.accountService.listAccounts().subscribe(accounts => {
      this.accounts = accounts;
    });
  }

  readonly form = this.fb.nonNullable.group({
    accountId: ['', Validators.required],
  });

  balance: BalanceDto | null = null;
  errorMessage: string | null = null;

  onCheckBalance(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.balance = null;
    this.errorMessage = null;

    const { accountId } = this.form.getRawValue();
    this.movementService.getBalance(accountId).subscribe({
      next: balance => {
        this.balance = balance;
      },
      error: (err: { message?: string }) => {
        this.errorMessage = err?.message ?? 'An unexpected error occurred.';
      },
    });
  }
}
