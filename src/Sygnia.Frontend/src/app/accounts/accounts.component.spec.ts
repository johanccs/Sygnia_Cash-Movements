import { TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { AccountsComponent } from './accounts.component';
import { AccountService, AccountDto } from '../services/account.service';

describe('AccountsComponent', () => {
  let accountServiceSpy: jasmine.SpyObj<AccountService>;

  beforeEach(async () => {
    accountServiceSpy = jasmine.createSpyObj('AccountService', ['createAccount', 'listAccounts']);
    accountServiceSpy.listAccounts.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [AccountsComponent, ReactiveFormsModule],
      providers: [{ provide: AccountService, useValue: accountServiceSpy }],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(AccountsComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('submits the form values to AccountService.createAccount and renders the created account id', () => {
    const createdAccount: AccountDto = {
      accountId: 'ACC-001',
      accountName: 'Acme Corp',
      contactPerson: 'Jane Doe',
      currency: 'ZAR',
      createdDate: new Date('2026-01-01T10:00:00Z'),
      createdBy: 'admin1',
    };
    accountServiceSpy.createAccount.and.returnValue(of(createdAccount));

    const fixture = TestBed.createComponent(AccountsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.setValue({
      accountId: 'ACC-001',
      accountName: 'Acme Corp',
      contactPerson: 'Jane Doe',
      currency: 'ZAR',
      createdBy: 'admin1',
    });
    component.onSubmit();
    fixture.detectChanges();

    expect(accountServiceSpy.createAccount).toHaveBeenCalledWith({
      accountId: 'ACC-001',
      accountName: 'Acme Corp',
      contactPerson: 'Jane Doe',
      currency: 'ZAR',
      createdBy: 'admin1',
    });

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('ACC-001');
  });

  it('shows the mapped error message when createAccount fails', () => {
    accountServiceSpy.createAccount.and.returnValue(throwError(() => ({ code: 6, message: 'already exists' })));

    const fixture = TestBed.createComponent(AccountsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.setValue({
      accountId: 'ACC-001',
      accountName: 'Acme Corp',
      contactPerson: 'Jane Doe',
      currency: 'ZAR',
      createdBy: 'admin1',
    });
    component.onSubmit();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('already exists');
  });

  it('renders the account list returned by AccountService.listAccounts', () => {
    accountServiceSpy.listAccounts.and.returnValue(
      of([
        {
          accountId: 'ACC-001',
          accountName: 'Acme Corp',
          contactPerson: 'Jane Doe',
          currency: 'ZAR',
          createdDate: new Date('2026-01-01T10:00:00Z'),
          createdBy: 'admin1',
        },
      ]),
    );

    const fixture = TestBed.createComponent(AccountsComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Acme Corp');
  });

  it('reloads the account list after a successful create', () => {
    const createdAccount: AccountDto = {
      accountId: 'ACC-002',
      accountName: 'Beacon Traders',
      contactPerson: 'John Doe',
      currency: 'ZAR',
      createdDate: new Date('2026-01-01T10:00:00Z'),
      createdBy: 'admin1',
    };
    accountServiceSpy.createAccount.and.returnValue(of(createdAccount));

    const fixture = TestBed.createComponent(AccountsComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    accountServiceSpy.listAccounts.calls.reset();

    component.form.setValue({
      accountId: 'ACC-002',
      accountName: 'Beacon Traders',
      contactPerson: 'John Doe',
      currency: 'ZAR',
      createdBy: 'admin1',
    });
    component.onSubmit();

    expect(accountServiceSpy.listAccounts).toHaveBeenCalled();
  });
});
