import { TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { UserComponent } from './user.component';
import { UserDto, UserService } from '../services/user.service';

describe('UserComponent', () => {
  let userServiceSpy: jasmine.SpyObj<UserService>;

  beforeEach(async () => {
    userServiceSpy = jasmine.createSpyObj('UserService', ['createUser', 'getUser']);

    await TestBed.configureTestingModule({
      imports: [UserComponent, ReactiveFormsModule],
      providers: [{ provide: UserService, useValue: userServiceSpy }],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(UserComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('submits the form values to UserService.createUser and renders the result', () => {
    const user: UserDto = { id: 'teller1', name: 'Jane', surname: 'Doe' };
    userServiceSpy.createUser.and.returnValue(of(user));

    const fixture = TestBed.createComponent(UserComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.setValue({ id: 'teller1', name: 'Jane', surname: 'Doe' });
    component.onSubmit();
    fixture.detectChanges();

    expect(userServiceSpy.createUser).toHaveBeenCalledWith({ id: 'teller1', name: 'Jane', surname: 'Doe' });

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Jane');
    expect(compiled.textContent).toContain('Doe');
    expect(compiled.textContent).toContain('teller1');
  });

  it('shows the mapped error message when createUser fails', () => {
    userServiceSpy.createUser.and.returnValue(throwError(() => ({ code: 6, message: 'user already exists' })));

    const fixture = TestBed.createComponent(UserComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.form.setValue({ id: 'teller1', name: 'Jane', surname: 'Doe' });
    component.onSubmit();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('user already exists');
  });

  it('does not call createUser when the form is invalid', () => {
    const fixture = TestBed.createComponent(UserComponent);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    component.onSubmit();

    expect(userServiceSpy.createUser).not.toHaveBeenCalled();
  });
});
