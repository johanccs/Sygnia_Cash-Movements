import { Directive, ElementRef, inject, input, OnDestroy, OnInit } from '@angular/core';

/** Bootstrap's own JS Tooltip class, loaded globally via angular.json's "scripts" array. */
interface BootstrapTooltipInstance {
  dispose(): void;
}
interface BootstrapGlobal {
  Tooltip: new (element: Element, options?: Record<string, unknown>) => BootstrapTooltipInstance;
}
declare const bootstrap: BootstrapGlobal;

/**
 * Thin wrapper around Bootstrap's Tooltip JS class — Bootstrap tooltips need explicit
 * `new bootstrap.Tooltip(el)` initialisation, they don't activate from a bare `title` attribute.
 * Usage: `<button appTooltip="Explanation text">`.
 */
@Directive({
  selector: '[appTooltip]',
  standalone: true,
})
export class TooltipDirective implements OnInit, OnDestroy {
  private readonly el = inject(ElementRef<HTMLElement>);
  private instance: BootstrapTooltipInstance | null = null;

  readonly appTooltip = input.required<string>();

  ngOnInit(): void {
    const nativeEl = this.el.nativeElement;
    nativeEl.setAttribute('title', this.appTooltip());
    nativeEl.setAttribute('data-bs-toggle', 'tooltip');
    this.instance = new bootstrap.Tooltip(nativeEl, { trigger: 'hover focus' });
  }

  ngOnDestroy(): void {
    this.instance?.dispose();
  }
}
