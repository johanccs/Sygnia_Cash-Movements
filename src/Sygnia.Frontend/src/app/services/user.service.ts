import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UserServiceClient } from '../grpc/UsersServiceClientPb';
import { CreateUserRequest, GetUserRequest, User } from '../grpc/users_pb';
import { fromGrpcCall, grpcServiceFactory } from './grpc-call.util';

/** Plain DTO mirroring the wire User message. */
export interface UserDto {
  id: string;
  name: string;
  surname: string;
}

export interface CreateUserInput {
  id: string;
  name: string;
  surname: string;
}

function mapUser(user: User): UserDto {
  return {
    id: user.getId(),
    name: user.getName(),
    surname: user.getSurname(),
  };
}

@Injectable({
  providedIn: 'root',
  useFactory: grpcServiceFactory(UserServiceClient, UserService),
})
export class UserService {
  constructor(private readonly client: UserServiceClient) {}

  createUser(input: CreateUserInput): Observable<UserDto> {
    const req = new CreateUserRequest();
    req.setId(input.id);
    req.setName(input.name);
    req.setSurname(input.surname);
    return fromGrpcCall((r, cb) => this.client.createUser(r, {}, cb), req, mapUser);
  }

  getUser(id: string): Observable<UserDto> {
    const req = new GetUserRequest();
    req.setId(id);
    return fromGrpcCall((r, cb) => this.client.getUser(r, {}, cb), req, mapUser);
  }
}
