import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AccountServiceClient } from '../grpc/AccountsServiceClientPb';
import {
  Account,
  CreateAccountRequest,
  GetAccountRequest,
  ListAccountsRequest,
  ListAccountsResponse,
} from '../grpc/accounts_pb';
import { environment } from '../../environments/environment';
import { fromGrpcCall } from './grpc-call.util';

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
  useFactory: () => new AccountService(new AccountServiceClient(environment.grpcUrl)),
})
export class AccountService {
  constructor(private readonly client: AccountServiceClient) {}

  createAccount(input: CreateAccountInput): Observable<AccountDto> {
    const req = new CreateAccountRequest();
    req.setAccountId(input.accountId);
    req.setAccountName(input.accountName);
    req.setContactPerson(input.contactPerson);
    req.setCurrency(input.currency);
    req.setCreatedBy(input.createdBy);
    return fromGrpcCall((r, cb) => this.client.createAccount(r, {}, cb), req, mapAccount);
  }

  getAccount(accountId: string): Observable<AccountDto> {
    const req = new GetAccountRequest();
    req.setAccountId(accountId);
    return fromGrpcCall((r, cb) => this.client.getAccount(r, {}, cb), req, mapAccount);
  }

  listAccounts(): Observable<AccountDto[]> {
    return fromGrpcCall(
      (r, cb) => this.client.listAccounts(r, {}, cb),
      new ListAccountsRequest(),
      (res: ListAccountsResponse) => res.getAccountsList().map(mapAccount),
    );
  }
}
