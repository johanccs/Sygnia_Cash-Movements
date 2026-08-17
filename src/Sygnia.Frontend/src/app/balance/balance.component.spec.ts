import { TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { BalanceComponent } from './balance.component';
import { MovementService } from '../services/movement.service';
import { AccountService } from '../services/account.service';

describe('BalanceComponent', () => {
  let movementServiceSpy: jasmine.SpyObj<MovementService>;
  let accountServiceSpy: jasmine.SpyObj<AccountService>;

  beforeEach(async () => {
    movementServiceSpy = jasmine.createSpyObj('MovementService', ['getBalance']);
    accountServiceSpy = jasmine.createSpyObj('AccountService', ['listAccounts']);
    accountServiceSpy.listAccounts.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [BalanceComponent, ReactiveFormsModule],
      providers: [
        { provide: MovementService, useValue: movementServiceSpy },
        { provide: AccountService, useValue: accountServiceSpy },
      ],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(BalanceComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('submits the account id to MovementService.getBalance and renders the balance', () => {
    movementServiceSpy.getBalance.and.returnValue(of({ accountId: 'ACC-001', balance: '1250.75' }));

    const fixture = TestBed.createComponent(BalanceComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.setValue({ accountId: 'ACC-001' });
    component.onCheckBalance();
    fixture.detectChanges();

    expect(movementServiceSpy.getBalance).toHaveBeenCalledWith('ACC-001');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('1250.75');
  });

  it('shows the mapped error message when getBalance fails', () => {
    movementServiceSpy.getBalance.and.returnValue(throwError(() => ({ code: 5, message: 'unknown account' })));

    const fixture = TestBed.createComponent(BalanceComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.setValue({ accountId: 'ACC-999' });
    component.onCheckBalance();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('unknown account');
  });

  it('does not call getBalance when the form is invalid', () => {
    const fixture = TestBed.createComponent(BalanceComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.onCheckBalance();

    expect(movementServiceSpy.getBalance).not.toHaveBeenCalled();
  });
});
