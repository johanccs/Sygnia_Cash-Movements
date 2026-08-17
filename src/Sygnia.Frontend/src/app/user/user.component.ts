import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { UserDto, UserService } from '../services/user.service';

/**
 * Creates the "normal user" record referenced by Movement.MovedBy — the person a submitted
 * movement or transfer is attributed to for audit purposes. Performing movements and checking
 * balance/statement are their own top-level pages now; this page only manages the user
 * records those actions get attributed to, not the actions themselves.
 */
@Component({
  selector: 'app-user',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './user.component.html',
  styleUrl: './user.component.scss',
})
export class UserComponent {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);

  readonly form = this.fb.nonNullable.group({
    id: ['', Validators.required],
    name: ['', Validators.required],
    surname: ['', Validators.required],
  });

  createdUser: UserDto | null = null;
  errorMessage: string | null = null;

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.createdUser = null;
    this.errorMessage = null;

    this.userService.createUser(this.form.getRawValue()).subscribe({
      next: user => {
        this.createdUser = user;
        this.form.reset();
      },
      error: (err: { message?: string }) => {
        this.errorMessage = err?.message ?? 'An unexpected error occurred.';
      },
    });
  }
}
