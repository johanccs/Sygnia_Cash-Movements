import { firstValueFrom } from 'rxjs';
import { UserService } from './user.service';
import { User } from '../grpc/users_pb';

describe('UserService', () => {
  it('maps a created User to a plain DTO', async () => {
    const client = jasmine.createSpyObj('UserServiceClient', ['createUser']);
    const proto = new User();
    proto.setId('teller1');
    proto.setName('Jane');
    proto.setSurname('Doe');
    client.createUser.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(null, proto));
    const service = new UserService(client);

    const result = await firstValueFrom(
      service.createUser({ id: 'teller1', name: 'Jane', surname: 'Doe' })
    );

    expect(result).toEqual({ id: 'teller1', name: 'Jane', surname: 'Doe' });
  });

  it('propagates errors from createUser', async () => {
    const client = jasmine.createSpyObj('UserServiceClient', ['createUser']);
    const rpcError = { code: 6, message: 'already exists' };
    client.createUser.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(rpcError, null));
    const service = new UserService(client);

    await expectAsync(
      firstValueFrom(service.createUser({ id: 'teller1', name: 'Jane', surname: 'Doe' }))
    ).toBeRejectedWith(rpcError);
  });

  it('maps GetUser response to a plain DTO', async () => {
    const client = jasmine.createSpyObj('UserServiceClient', ['getUser']);
    const proto = new User();
    proto.setId('teller1');
    proto.setName('Jane');
    proto.setSurname('Doe');
    client.getUser.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(null, proto));
    const service = new UserService(client);

    const result = await firstValueFrom(service.getUser('teller1'));

    expect(result).toEqual({ id: 'teller1', name: 'Jane', surname: 'Doe' });
  });

  it('propagates errors from getUser', async () => {
    const client = jasmine.createSpyObj('UserServiceClient', ['getUser']);
    const rpcError = { code: 5, message: 'not found' };
    client.getUser.and.callFake((_req: unknown, _meta: unknown, cb: Function) => cb(rpcError, null));
    const service = new UserService(client);

    await expectAsync(firstValueFrom(service.getUser('unknown'))).toBeRejectedWith(rpcError);
  });
});
