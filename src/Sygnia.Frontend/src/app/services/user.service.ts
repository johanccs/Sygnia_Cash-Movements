import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { UserServiceClient } from '../grpc/UsersServiceClientPb';
import { CreateUserRequest, GetUserRequest, User } from '../grpc/users_pb';

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

@Injectable({ providedIn: 'root' })
export class UserService {
  constructor(
    private readonly client: UserServiceClient = new UserServiceClient('http://localhost:5000')
  ) {}

  createUser(input: CreateUserInput): Observable<UserDto> {
    return new Observable(observer => {
      const req = new CreateUserRequest();
      req.setId(input.id);
      req.setName(input.name);
      req.setSurname(input.surname);
      this.client.createUser(req, {}, (err, res) => {
        if (err) {
          observer.error(err);
          return;
        }
        observer.next(mapUser(res));
        observer.complete();
      });
    });
  }

  getUser(id: string): Observable<UserDto> {
    return new Observable(observer => {
      const req = new GetUserRequest();
      req.setId(id);
      this.client.getUser(req, {}, (err, res) => {
        if (err) {
          observer.error(err);
          return;
        }
        observer.next(mapUser(res));
        observer.complete();
      });
    });
  }
}
