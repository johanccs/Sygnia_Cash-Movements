import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AccountServiceClient } from '../grpc/AccountsServiceClientPb';
import { Account, CreateAccountRequest, GetAccountRequest } from '../grpc/accounts_pb';

/** Plain DTO mirroring the wire Account message, with Timestamps converted to JS Dates. */
export interface AccountDto {
  accountId: string;
  accountName: string;
  contactPerson: string;
  currency: string;
  createdDate: Date | null;
  createdBy: string;
}

export interface CreateAccountInput {
  accountId: string;
  accountName: string;
  contactPerson: string;
  currency: string;
  createdBy: string;
}

function mapAccount(account: Account): AccountDto {
  return {
    accountId: account.getAccountId(),
    accountName: account.getAccountName(),
    contactPerson: account.getContactPerson(),
    currency: account.getCurrency(),
    createdDate: account.getCreatedDate()?.toDate() ?? null,
    createdBy: account.getCreatedBy(),
  };
}

@Injectable({
  providedIn: 'root',
  useFactory: () => new AccountService(new AccountServiceClient('http://localhost:5000')),
})
export class AccountService {
  constructor(private readonly client: AccountServiceClient) {}

  createAccount(input: CreateAccountInput): Observable<AccountDto> {
    return new Observable(observer => {
      const req = new CreateAccountRequest();
      req.setAccountId(input.accountId);
      req.setAccountName(input.accountName);
      req.setContactPerson(input.contactPerson);
      req.setCurrency(input.currency);
      req.setCreatedBy(input.createdBy);
      this.client.createAccount(req, {}, (err, res) => {
        if (err) {
          observer.error(err);
          return;
        }
        observer.next(mapAccount(res));
        observer.complete();
      });
    });
  }

  getAccount(accountId: string): Observable<AccountDto> {
    return new Observable(observer => {
      const req = new GetAccountRequest();
      req.setAccountId(accountId);
      this.client.getAccount(req, {}, (err, res) => {
        if (err) {
          observer.error(err);
          return;
        }
        observer.next(mapAccount(res));
        observer.complete();
      });
    });
  }
}
