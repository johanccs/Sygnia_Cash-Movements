import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MovementDto, MovementService, TransferResultDto } from '../services/movement.service';
import { AccountDto, AccountService } from '../services/account.service';
import { UserDto, UserService } from '../services/user.service';
import { MAJOR_CURRENCIES } from '../shared/currencies';

type Tab = 'submit' | 'transfer';

/**
 * refNr is auto-generated client-side via crypto.randomUUID() rather than typed by the user —
 * it is a GUID identifying the movement itself, not something a user would reasonably compose.
 */
@Component({
  selector: 'app-movement',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './movement.component.html',
  styleUrl: './movement.component.scss',
})
export class MovementComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly movementService = inject(MovementService);
  private readonly accountService = inject(AccountService);
  private readonly userService = inject(UserService);

  readonly currencies = MAJOR_CURRENCIES;
  accounts: AccountDto[] = [];
  users: UserDto[] = [];

  activeTab: Tab = 'submit';

  ngOnInit(): void {
    this.accountService.listAccounts().subscribe(accounts => {
      this.accounts = accounts;
    });
    this.userService.listUsers().subscribe(users => {
      this.users = users;
    });
  }

  setActiveTab(tab: Tab): void {
    this.activeTab = tab;
  }

  readonly submitForm = this.fb.nonNullable.group({
    accountId: ['', Validators.required],
    externalRef: ['', Validators.required],
    currency: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    amount: ['', Validators.required],
    occurredAt: ['', Validators.required],
    narration: ['', Validators.required],
    refNr: [crypto.randomUUID() as string, Validators.required],
    movedBy: ['', Validators.required],
    movedDate: [new Date().toISOString().slice(0, 10), Validators.required],
  });

  submittedMovement: MovementDto | null = null;
  submitErrorMessage: string | null = null;

  onSubmitMovement(): void {
    if (this.submitForm.invalid) {
      this.submitForm.markAllAsTouched();
      return;
    }

    this.submittedMovement = null;
    this.submitErrorMessage = null;

    const raw = this.submitForm.getRawValue();
    this.movementService
      .submitMovement({
        accountId: raw.accountId,
        externalRef: raw.externalRef,
        currency: raw.currency,
        amount: raw.amount,
        occurredAt: new Date(raw.occurredAt),
        narration: raw.narration,
        refNr: raw.refNr,
        movedBy: raw.movedBy,
        movedDate: new Date(raw.movedDate),
      })
      .subscribe({
        next: movement => {
          this.submittedMovement = movement;
        },
        error: (err: { message?: string }) => {
          this.submitErrorMessage = err?.message ?? 'An unexpected error occurred.';
        },
      });
  }

  readonly transferForm = this.fb.nonNullable.group({
    fromAccountId: ['', Validators.required],
    toAccountId: ['', Validators.required],
    externalRef: ['', Validators.required],
    currency: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
    amount: ['', Validators.required],
    occurredAt: ['', Validators.required],
    narration: ['', Validators.required],
    refNr: [crypto.randomUUID() as string, Validators.required],
    movedBy: ['', Validators.required],
    movedDate: [new Date().toISOString().slice(0, 10), Validators.required],
  });

  transferResult: TransferResultDto | null = null;
  transferErrorMessage: string | null = null;

  onTransfer(): void {
    if (this.transferForm.invalid) {
      this.transferForm.markAllAsTouched();
      return;
    }

    this.transferResult = null;
    this.transferErrorMessage = null;

    const raw = this.transferForm.getRawValue();
    this.movementService
      .transfer({
        fromAccountId: raw.fromAccountId,
        toAccountId: raw.toAccountId,
        externalRef: raw.externalRef,
        currency: raw.currency,
        amount: raw.amount,
        occurredAt: new Date(raw.occurredAt),
        narration: raw.narration,
        refNr: raw.refNr,
        movedBy: raw.movedBy,
        movedDate: new Date(raw.movedDate),
      })
      .subscribe({
        next: result => {
          this.transferResult = result;
        },
        error: (err: { message?: string }) => {
          this.transferErrorMessage = err?.message ?? 'An unexpected error occurred.';
        },
      });
  }
}
