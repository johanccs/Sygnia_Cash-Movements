import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

declare global {
  interface Window {
    __env?: { grpcUrl?: string };
  }
}

/**
 * The gRPC target is a runtime concern, not a build-time one: the same frontend image is
 * reused by run-local.ps1 (native Presentation on :5058) and run-dockerhub.ps1 (the
 * `presentation` container on :8080). window.__env is populated by nginx's entrypoint
 * script from the GRPC_URL env var at container start (see nginx.conf); the compiled
 * `environment.grpcUrl` is only the fallback for `ng serve`.
 */
function resolveGrpcUrl(): string {
  return window.__env?.grpcUrl || environment.grpcUrl;
}

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
  return () => new ServiceCtor(new ClientCtor(resolveGrpcUrl()));
}
