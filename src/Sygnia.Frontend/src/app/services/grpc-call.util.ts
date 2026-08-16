import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Wraps the generated gRPC-Web client's callback-style unary call
 * (`client.method(req, {}, (err, res) => {...})`) as an Observable,
 * mapping the response through `mapFn`. Shared by all services under
 * `services/` to avoid repeating the same Observable/callback boilerplate.
 */
export function fromGrpcCall<TReq, TRes, TDto>(
  invoke: (request: TReq, callback: (err: unknown, res: TRes) => void) => void,
  request: TReq,
  mapFn: (res: TRes) => TDto,
): Observable<TDto> {
  return new Observable(observer => {
    invoke(request, (err, res) => {
      if (err) {
        observer.error(err);
        return;
      }
      observer.next(mapFn(res));
      observer.complete();
    });
  });
}

/**
 * Builds an Angular `useFactory` that constructs `client, () => new Service(new Client(environment.grpcUrl))`.
 * Shared by all services under `services/` to avoid repeating the same DI-factory boilerplate.
 */
export function grpcServiceFactory<TClient, TService>(
  ClientCtor: new (url: string) => TClient,
  ServiceCtor: new (client: TClient) => TService,
): () => TService {
  return () => new ServiceCtor(new ClientCtor(environment.grpcUrl));
}
