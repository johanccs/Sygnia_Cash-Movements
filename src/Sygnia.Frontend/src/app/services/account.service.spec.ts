import { firstValueFrom } from 'rxjs';
import { Timestamp } from 'google-protobuf/google/protobuf/timestamp_pb';
import { AccountService } from './account.service';
import { Account } from '../grpc/accounts_pb';

describe('AccountService', () => {
  it('maps a created Account to a plain DTO', async () => {
    const client = jasmine.createSpyObj('AccountServiceClient', ['createAccount']);
    const createdDate = new Date('2026-01-01T10:00:00Z');
    const proto = new Account();
    proto.setAccountId('ACC-001');
    proto.setAccountName('Acme Corp');
    proto.setContactPerson('Jane Doe');
    proto.setCurrency('ZAR');
    proto.setCreatedDate(Timestamp.fromDate(createdDate));
    proto.setCreatedBy('admin1');
    client.createAccount.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(null, proto));
    const service = new AccountService(client);

    const result = await firstValueFrom(
      service.createAccount({
        accountId: 'ACC-001',
        accountName: 'Acme Corp',
        contactPerson: 'Jane Doe',
        currency: 'ZAR',
        createdBy: 'admin1',
      })
    );

    expect(result).toEqual({
      accountId: 'ACC-001',
      accountName: 'Acme Corp',
      contactPerson: 'Jane Doe',
      currency: 'ZAR',
      createdDate,
      createdBy: 'admin1',
    });
  });

  it('propagates errors from createAccount', async () => {
    const client = jasmine.createSpyObj('AccountServiceClient', ['createAccount']);
    const rpcError = { code: 6, message: 'already exists' };
    client.createAccount.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(rpcError, null));
    const service = new AccountService(client);

    await expectAsync(
      firstValueFrom(
        service.createAccount({
          accountId: 'ACC-001',
          accountName: 'Acme Corp',
          contactPerson: 'Jane Doe',
          currency: 'ZAR',
          createdBy: 'admin1',
        })
      )
    ).toBeRejectedWith(rpcError);
  });

  it('maps GetAccount response to a plain DTO', async () => {
    const client = jasmine.createSpyObj('AccountServiceClient', ['getAccount']);
    const proto = new Account();
    proto.setAccountId('ACC-001');
    proto.setAccountName('Acme Corp');
    proto.setContactPerson('Jane Doe');
    proto.setCurrency('ZAR');
    proto.setCreatedBy('admin1');
    // createdDate deliberately left unset to exercise the null fallback.
    client.getAccount.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(null, proto));
    const service = new AccountService(client);

    const result = await firstValueFrom(service.getAccount('ACC-001'));

    expect(result.accountId).toBe('ACC-001');
    expect(result.accountName).toBe('Acme Corp');
    expect(result.createdDate).toBeNull();
  });

  it('propagates errors from getAccount', async () => {
    const client = jasmine.createSpyObj('AccountServiceClient', ['getAccount']);
    const rpcError = { code: 5, message: 'not found' };
    client.getAccount.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(rpcError, null));
    const service = new AccountService(client);

    await expectAsync(firstValueFrom(service.getAccount('ACC-404'))).toBeRejectedWith(rpcError);
  });
});
