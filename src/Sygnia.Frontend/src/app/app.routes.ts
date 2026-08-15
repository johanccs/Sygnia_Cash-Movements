import { Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { AccountsComponent } from './accounts/accounts.component';
import { StatementComponent } from './statement/statement.component';
import { UserComponent } from './user/user.component';

export const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'accounts', component: AccountsComponent },
  { path: 'statement', component: StatementComponent },
  { path: 'user', component: UserComponent },
];
