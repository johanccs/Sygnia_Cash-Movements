import { firstValueFrom } from 'rxjs';
import { Timestamp } from 'google-protobuf/google/protobuf/timestamp_pb';
import { MovementService } from './movement.service';
import {
  GetBalanceResponse,
  GetStatementPageResponse,
  Movement,
  StatementLine,
  TransferResponse,
} from '../grpc/movements_pb';

describe('MovementService', () => {
  it('maps GetBalanceResponse to a plain DTO', async () => {
    const client = jasmine.createSpyObj('MovementServiceClient', ['getBalance']);
    const proto = new GetBalanceResponse();
    proto.setAccountId('ACC-001');
    proto.setBalance('100.00');
    client.getBalance.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(null, proto));
    const service = new MovementService(client);

    const result = await firstValueFrom(service.getBalance('ACC-001'));

    expect(result).toEqual({ accountId: 'ACC-001', balance: '100.00' });
  });

  it('propagates errors from getBalance', async () => {
    const client = jasmine.createSpyObj('MovementServiceClient', ['getBalance']);
    const rpcError = { code: 5, message: 'not found' };
    client.getBalance.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(rpcError, null));
    const service = new MovementService(client);

    await expectAsync(firstValueFrom(service.getBalance('ACC-404'))).toBeRejectedWith(rpcError);
  });

  it('maps a submitted Movement to a plain DTO', async () => {
    const client = jasmine.createSpyObj('MovementServiceClient', ['submitMovement']);
    const occurredAt = new Date('2026-01-01T10:00:00Z');
    const movedDate = new Date('2026-01-01T10:05:00Z');
    const proto = new Movement();
    proto.setAccountId('ACC-001');
    proto.setExternalRef('MOV-20260101-000001');
    proto.setCurrency('ZAR');
    proto.setAmount('50.00');
    proto.setOccurredAt(Timestamp.fromDate(occurredAt));
    proto.setNarration('Deposit');
    proto.setRefNr('11111111-1111-1111-1111-111111111111');
    proto.setMovedBy('teller1');
    proto.setMovedDate(Timestamp.fromDate(movedDate));
    client.submitMovement.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(null, proto));
    const service = new MovementService(client);

    const result = await firstValueFrom(
      service.submitMovement({
        accountId: 'ACC-001',
        externalRef: 'MOV-20260101-000001',
        currency: 'ZAR',
        amount: '50.00',
        occurredAt,
        narration: 'Deposit',
        refNr: '11111111-1111-1111-1111-111111111111',
        movedBy: 'teller1',
        movedDate,
      })
    );

    expect(result).toEqual({
      accountId: 'ACC-001',
      externalRef: 'MOV-20260101-000001',
      currency: 'ZAR',
      amount: '50.00',
      occurredAt,
      narration: 'Deposit',
      refNr: '11111111-1111-1111-1111-111111111111',
      movedBy: 'teller1',
      movedDate,
    });
  });

  it('propagates errors from submitMovement', async () => {
    const client = jasmine.createSpyObj('MovementServiceClient', ['submitMovement']);
    const rpcError = { code: 6, message: 'already exists' };
    client.submitMovement.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(rpcError, null));
    const service = new MovementService(client);

    await expectAsync(
      firstValueFrom(
        service.submitMovement({
          accountId: 'ACC-001',
          externalRef: 'MOV-1',
          currency: 'ZAR',
          amount: '50.00',
          occurredAt: new Date(),
          narration: '',
          refNr: 'r',
          movedBy: 'teller1',
          movedDate: new Date(),
        })
      )
    ).toBeRejectedWith(rpcError);
  });

  it('maps TransferResponse debit/credit legs to plain DTOs', async () => {
    const client = jasmine.createSpyObj('MovementServiceClient', ['transfer']);
    const debit = new Movement();
    debit.setAccountId('ACC-001');
    debit.setAmount('-25.00');
    const credit = new Movement();
    credit.setAccountId('ACC-002');
    credit.setAmount('25.00');
    const proto = new TransferResponse();
    proto.setDebit(debit);
    proto.setCredit(credit);
    client.transfer.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(null, proto));
    const service = new MovementService(client);

    const result = await firstValueFrom(
      service.transfer({
        fromAccountId: 'ACC-001',
        toAccountId: 'ACC-002',
        externalRef: 'MOV-1',
        currency: 'ZAR',
        amount: '25.00',
        occurredAt: new Date(),
        narration: '',
        refNr: 'r',
        movedBy: 'teller1',
        movedDate: new Date(),
      })
    );

    expect(result.debit?.accountId).toBe('ACC-001');
    expect(result.debit?.amount).toBe('-25.00');
    expect(result.credit?.accountId).toBe('ACC-002');
    expect(result.credit?.amount).toBe('25.00');
  });

  it('propagates errors from transfer', async () => {
    const client = jasmine.createSpyObj('MovementServiceClient', ['transfer']);
    const rpcError = { code: 3, message: 'invalid argument' };
    client.transfer.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(rpcError, null));
    const service = new MovementService(client);

    await expectAsync(
      firstValueFrom(
        service.transfer({
          fromAccountId: 'ACC-001',
          toAccountId: 'ACC-002',
          externalRef: 'MOV-1',
          currency: 'ZAR',
          amount: '25.00',
          occurredAt: new Date(),
          narration: '',
          refNr: 'r',
          movedBy: 'teller1',
          movedDate: new Date(),
        })
      )
    ).toBeRejectedWith(rpcError);
  });

  it('maps GetStatementPageResponse to a plain DTO, leaving paged runningTotal null when unset', async () => {
    const client = jasmine.createSpyObj('MovementServiceClient', ['getStatementPage']);
    const movement = new Movement();
    movement.setAccountId('ACC-001');
    movement.setAmount('10.00');
    const line = new StatementLine();
    line.setMovement(movement);
    // running_total deliberately left unset for paged results.
    const proto = new GetStatementPageResponse();
    proto.setLinesList([line]);
    proto.setTotalCount(1);
    client.getStatementPage.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(null, proto));
    const service = new MovementService(client);

    const result = await firstValueFrom(
      service.getStatementPage({ accountId: 'ACC-001', pageNumber: 1, pageSize: 20 })
    );

    expect(result.totalCount).toBe(1);
    expect(result.lines.length).toBe(1);
    expect(result.lines[0].movement?.accountId).toBe('ACC-001');
    expect(result.lines[0].runningTotal).toBeNull();
  });

  it('propagates errors from getStatementPage', async () => {
    const client = jasmine.createSpyObj('MovementServiceClient', ['getStatementPage']);
    const rpcError = { code: 5, message: 'not found' };
    client.getStatementPage.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(rpcError, null));
    const service = new MovementService(client);

    await expectAsync(
      firstValueFrom(service.getStatementPage({ accountId: 'ACC-404', pageNumber: 1, pageSize: 20 }))
    ).toBeRejectedWith(rpcError);
  });
});
