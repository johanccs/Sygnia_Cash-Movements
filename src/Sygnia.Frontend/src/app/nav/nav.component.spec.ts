import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RouterLink } from '@angular/router';
import { routes } from '../app.routes';
import { NavComponent } from './nav.component';

describe('NavComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NavComponent],
      providers: [provideRouter(routes)],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(NavComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('renders routerLinks for Home, Accounts, Movement, Balance, Statement, and User', () => {
    const fixture = TestBed.createComponent(NavComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    const links = Array.from(
      compiled.querySelectorAll('a.nav-link[routerLink]'),
    ) as HTMLAnchorElement[];
    const linkTargets = links.map(link => link.getAttribute('routerLink'));

    expect(linkTargets).toContain('/');
    expect(linkTargets).toContain('/accounts');
    expect(linkTargets).toContain('/movement');
    expect(linkTargets).toContain('/balance');
    expect(linkTargets).toContain('/user');
    expect(linkTargets).toContain('/statement');
    expect(links.length).toBe(6);
  });

  it('gives each nav link routerLinkActive="active"', () => {
    const fixture = TestBed.createComponent(NavComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    const links = Array.from(compiled.querySelectorAll('a.nav-link[routerLink]'));
    links.forEach(link => {
      expect(link.getAttribute('routerLinkActive')).toBe('active');
    });
  });

  it('renders a nav landmark with an accessible label', () => {
    const fixture = TestBed.createComponent(NavComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    const nav = compiled.querySelector('nav');
    expect(nav).toBeTruthy();
    expect(nav?.getAttribute('aria-label')).toBeTruthy();
  });
});
