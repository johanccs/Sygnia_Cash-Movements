import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { By } from '@angular/platform-browser';
import { StatementComponent } from './statement.component';
import { GetStatementPageInput, MovementService, StatementPageDto } from '../services/movement.service';
import { AccountService } from '../services/account.service';

describe('StatementComponent', () => {
  let fixture: ComponentFixture<StatementComponent>;
  let component: StatementComponent;
  let movementServiceSpy: jasmine.SpyObj<MovementService>;
  let accountServiceSpy: jasmine.SpyObj<AccountService>;

  const page: StatementPageDto = {
    lines: [
      {
        movement: {
          accountId: 'ACC-001',
          externalRef: 'MOV-20240715-000123',
          currency: 'ZAR',
          amount: '100.00',
          occurredAt: new Date('2024-07-15T00:00:00Z'),
          narration: 'Deposit',
          refNr: 'abc-123',
          movedBy: 'teller1',
          movedDate: new Date('2024-07-15T00:00:00Z'),
        },
        runningTotal: null,
      },
    ],
    totalCount: 1,
  };

  beforeEach(async () => {
    movementServiceSpy = jasmine.createSpyObj('MovementService', ['getStatementPage']);
    movementServiceSpy.getStatementPage.and.returnValue(of(page));
    accountServiceSpy = jasmine.createSpyObj('AccountService', ['listAccounts']);
    accountServiceSpy.listAccounts.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [StatementComponent, ReactiveFormsModule],
      providers: [
        { provide: MovementService, useValue: movementServiceSpy },
        { provide: AccountService, useValue: accountServiceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(StatementComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('calls getStatementPage with page 1 when search() is invoked with account id and date range', () => {
    component.form.setValue({
      accountId: 'ACC-001',
      from: '2024-07-01',
      to: '2024-07-31',
    });

    component.search();
    fixture.detectChanges();

    expect(movementServiceSpy.getStatementPage).toHaveBeenCalled();
    const input: GetStatementPageInput = movementServiceSpy.getStatementPage.calls.mostRecent().args[0];
    expect(input.accountId).toBe('ACC-001');
    expect(input.pageNumber).toBe(1);
    expect(input.from).toEqual(new Date('2024-07-01'));
    expect(input.to).toEqual(new Date('2024-07-31'));
  });

  it('renders the returned lines via app-statement-preview', () => {
    component.form.setValue({ accountId: 'ACC-001', from: '', to: '' });
    component.search();
    fixture.detectChanges();

    expect(component.currentPage()?.lines).toEqual(page.lines);
    const preview = fixture.debugElement.query(By.css('app-statement-preview'));
    expect(preview).toBeTruthy();
  });

  it('shows an error message when the service call fails', () => {
    movementServiceSpy.getStatementPage.and.returnValue(throwError(() => ({ message: 'boom' })));
    component.form.setValue({ accountId: 'ACC-001', from: '', to: '' });

    component.search();
    fixture.detectChanges();

    expect(component.errorMessage).toBe('boom');
  });

  it('goToPage re-calls getStatementPage with the requested page number', () => {
    movementServiceSpy.getStatementPage.and.returnValue(of({ lines: page.lines, totalCount: 30 }));
    component.form.setValue({ accountId: 'ACC-001', from: '', to: '' });
    component.search();
    fixture.detectChanges();

    movementServiceSpy.getStatementPage.calls.reset();
    movementServiceSpy.getStatementPage.and.returnValue(of({ lines: [], totalCount: 30 }));

    component.goToPage(2);

    const input: GetStatementPageInput = movementServiceSpy.getStatementPage.calls.mostRecent().args[0];
    expect(input.pageNumber).toBe(2);
  });
});
