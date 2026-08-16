import { Observable } from 'rxjs';

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
