import { Injectable, signal } from '@angular/core';

export interface ToastMessage {
  id: number;
  text: string;
}

/**
 * Minimal signal-based toast queue, rendered by ToastContainerComponent. Deliberately not
 * wrapping Bootstrap's JS Toast class (new bootstrap.Toast(el).show()) — that needs a real DOM
 * element to exist before it can be instantiated, which fights Angular's own rendering timing.
 * Auto-dismiss is a plain timer instead.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;
  readonly messages = signal<ToastMessage[]>([]);

  show(text: string, durationMs = 3000): void {
    const id = this.nextId++;
    this.messages.update(messages => [...messages, { id, text }]);
    setTimeout(() => this.dismiss(id), durationMs);
  }

  dismiss(id: number): void {
    this.messages.update(messages => messages.filter(m => m.id !== id));
  }
}
