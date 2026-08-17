import { TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { MovementComponent } from './movement.component';
import { MovementDto, MovementService } from '../services/movement.service';
import { AccountService } from '../services/account.service';
import { UserService } from '../services/user.service';

describe('MovementComponent', () => {
  let movementServiceSpy: jasmine.SpyObj<MovementService>;
  let accountServiceSpy: jasmine.SpyObj<AccountService>;
  let userServiceSpy: jasmine.SpyObj<UserService>;

  beforeEach(async () => {
    movementServiceSpy = jasmine.createSpyObj('MovementService', ['submitMovement', 'transfer']);
    accountServiceSpy = jasmine.createSpyObj('AccountService', ['listAccounts']);
    accountServiceSpy.listAccounts.and.returnValue(of([]));
    userServiceSpy = jasmine.createSpyObj('UserService', ['listUsers']);
    userServiceSpy.listUsers.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [MovementComponent, ReactiveFormsModule],
      providers: [
        { provide: MovementService, useValue: movementServiceSpy },
        { provide: AccountService, useValue: accountServiceSpy },
        { provide: UserService, useValue: userServiceSpy },
      ],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(MovementComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('defaults to the Submit Movement tab', () => {
    const fixture = TestBed.createComponent(MovementComponent);
    expect(fixture.componentInstance.activeTab).toBe('submit');
  });

  it('populates the Moved By dropdown from UserService and stores the selected user id', () => {
    userServiceSpy.listUsers.and.returnValue(
      of([
        { id: 'teller1', name: 'Jane', surname: 'Doe' },
        { id: 'teller2', name: 'John', surname: 'Smith' },
      ]),
    );

    const fixture = TestBed.createComponent(MovementComponent);
    fixture.detectChanges();

    const select: HTMLSelectElement = fixture.nativeElement.querySelector('#submitMovedBy');
    const options = Array.from(select.options).map(o => o.value);
    expect(options).toContain('teller1');
    expect(options).toContain('teller2');

    select.value = 'teller2';
    select.dispatchEvent(new Event('change'));

    expect(fixture.componentInstance.submitForm.value.movedBy).toBe('teller2');
  });

  it('switches tabs when setActiveTab is called', () => {
    const fixture = TestBed.createComponent(MovementComponent);
    const component = fixture.componentInstance;

    component.setActiveTab('transfer');
    expect(component.activeTab).toBe('transfer');
  });

  describe('Submit Movement pane', () => {
    it('submits the form values to MovementService.submitMovement and renders the result', () => {
      const movement: MovementDto = {
        accountId: 'ACC-001',
        externalRef: 'MOV-20260815-000001',
        currency: 'ZAR',
        amount: '125.50',
        occurredAt: new Date('2026-08-15T00:00:00Z'),
        narration: 'Deposit',
        refNr: 'ref-123',
        movedBy: 'admin1',
        movedDate: new Date('2026-08-15T00:00:00Z'),
      };
      movementServiceSpy.submitMovement.and.returnValue(of(movement));

      const fixture = TestBed.createComponent(MovementComponent);
      const component = fixture.componentInstance;
      fixture.detectChanges();

      component.submitForm.setValue({
        accountId: 'ACC-001',
        externalRef: 'MOV-20260815-000001',
        currency: 'ZAR',
        amount: '125.50',
        occurredAt: '2026-08-15',
        narration: 'Deposit',
        refNr: 'ref-123',
        movedBy: 'admin1',
        movedDate: '2026-08-15',
      });
      component.onSubmitMovement();
      fixture.detectChanges();

      expect(movementServiceSpy.submitMovement).toHaveBeenCalledWith({
        accountId: 'ACC-001',
        externalRef: 'MOV-20260815-000001',
        currency: 'ZAR',
        amount: '125.50',
        occurredAt: new Date('2026-08-15'),
        narration: 'Deposit',
        refNr: 'ref-123',
        movedBy: 'admin1',
        movedDate: new Date('2026-08-15'),
      });

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('MOV-20260815-000001');
    });

    it('shows the mapped error message when submitMovement fails', () => {
      movementServiceSpy.submitMovement.and.returnValue(
        throwError(() => ({ code: 6, message: 'duplicate external ref' })),
      );

      const fixture = TestBed.createComponent(MovementComponent);
      const component = fixture.componentInstance;
      fixture.detectChanges();

      component.submitForm.setValue({
        accountId: 'ACC-001',
        externalRef: 'MOV-20260815-000001',
        currency: 'ZAR',
        amount: '125.50',
        occurredAt: '2026-08-15',
        narration: 'Deposit',
        refNr: 'ref-123',
        movedBy: 'admin1',
        movedDate: '2026-08-15',
      });
      component.onSubmitMovement();
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('duplicate external ref');
    });

    it('does not call submitMovement when the form is invalid', () => {
      const fixture = TestBed.createComponent(MovementComponent);
      const component = fixture.componentInstance;
      fixture.detectChanges();

      component.onSubmitMovement();

      expect(movementServiceSpy.submitMovement).not.toHaveBeenCalled();
    });
  });

  describe('Transfer pane', () => {
    it('submits the form values to MovementService.transfer and renders the result', () => {
      const debit: MovementDto = {
        accountId: 'ACC-001',
        externalRef: 'MOV-20260815-000002',
        currency: 'ZAR',
        amount: '-50.00',
        occurredAt: new Date('2026-08-15T00:00:00Z'),
        narration: 'Transfer out',
        refNr: 'ref-456',
        movedBy: 'admin1',
        movedDate: new Date('2026-08-15T00:00:00Z'),
      };
      const credit: MovementDto = { ...debit, accountId: 'ACC-002', amount: '50.00' };
      movementServiceSpy.transfer.and.returnValue(of({ debit, credit }));

      const fixture = TestBed.createComponent(MovementComponent);
      const component = fixture.componentInstance;
      component.setActiveTab('transfer');
      fixture.detectChanges();

      component.transferForm.setValue({
        fromAccountId: 'ACC-001',
        toAccountId: 'ACC-002',
        externalRef: 'MOV-20260815-000002',
        currency: 'ZAR',
        amount: '50.00',
        occurredAt: '2026-08-15',
        narration: 'Transfer out',
        refNr: 'ref-456',
        movedBy: 'admin1',
        movedDate: '2026-08-15',
      });
      component.onTransfer();
      fixture.detectChanges();

      expect(movementServiceSpy.transfer).toHaveBeenCalledWith({
        fromAccountId: 'ACC-001',
        toAccountId: 'ACC-002',
        externalRef: 'MOV-20260815-000002',
        currency: 'ZAR',
        amount: '50.00',
        occurredAt: new Date('2026-08-15'),
        narration: 'Transfer out',
        refNr: 'ref-456',
        movedBy: 'admin1',
        movedDate: new Date('2026-08-15'),
      });

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('ACC-001');
      expect(compiled.textContent).toContain('ACC-002');
    });

    it('shows the mapped error message when transfer fails', () => {
      movementServiceSpy.transfer.and.returnValue(throwError(() => ({ code: 5, message: 'unknown account' })));

      const fixture = TestBed.createComponent(MovementComponent);
      const component = fixture.componentInstance;
      component.setActiveTab('transfer');
      fixture.detectChanges();

      component.transferForm.setValue({
        fromAccountId: 'ACC-001',
        toAccountId: 'ACC-002',
        externalRef: 'MOV-20260815-000002',
        currency: 'ZAR',
        amount: '50.00',
        occurredAt: '2026-08-15',
        narration: 'Transfer out',
        refNr: 'ref-456',
        movedBy: 'admin1',
        movedDate: '2026-08-15',
      });
      component.onTransfer();
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('unknown account');
    });

    it('does not call transfer when the form is invalid', () => {
      const fixture = TestBed.createComponent(MovementComponent);
      const component = fixture.componentInstance;
      fixture.detectChanges();

      component.onTransfer();

      expect(movementServiceSpy.transfer).not.toHaveBeenCalled();
    });
  });
});
