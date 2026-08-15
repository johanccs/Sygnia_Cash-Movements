import * as jspb from 'google-protobuf'

import * as google_protobuf_timestamp_pb from 'google-protobuf/google/protobuf/timestamp_pb'; // proto import: "google/protobuf/timestamp.proto"


export class Account extends jspb.Message {
  getAccountId(): string;
  setAccountId(value: string): Account;

  getAccountName(): string;
  setAccountName(value: string): Account;

  getContactPerson(): string;
  setContactPerson(value: string): Account;

  getCurrency(): string;
  setCurrency(value: string): Account;

  getCreatedDate(): google_protobuf_timestamp_pb.Timestamp | undefined;
  setCreatedDate(value?: google_protobuf_timestamp_pb.Timestamp): Account;
  hasCreatedDate(): boolean;
  clearCreatedDate(): Account;

  getCreatedBy(): string;
  setCreatedBy(value: string): Account;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): Account.AsObject;
  static toObject(includeInstance: boolean, msg: Account): Account.AsObject;
  static serializeBinaryToWriter(message: Account, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): Account;
  static deserializeBinaryFromReader(message: Account, reader: jspb.BinaryReader): Account;
}

export namespace Account {
  export type AsObject = {
    accountId: string,
    accountName: string,
    contactPerson: string,
    currency: string,
    createdDate?: google_protobuf_timestamp_pb.Timestamp.AsObject,
    createdBy: string,
  }
}

export class CreateAccountRequest extends jspb.Message {
  getAccountId(): string;
  setAccountId(value: string): CreateAccountRequest;

  getAccountName(): string;
  setAccountName(value: string): CreateAccountRequest;

  getContactPerson(): string;
  setContactPerson(value: string): CreateAccountRequest;

  getCurrency(): string;
  setCurrency(value: string): CreateAccountRequest;

  getCreatedBy(): string;
  setCreatedBy(value: string): CreateAccountRequest;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): CreateAccountRequest.AsObject;
  static toObject(includeInstance: boolean, msg: CreateAccountRequest): CreateAccountRequest.AsObject;
  static serializeBinaryToWriter(message: CreateAccountRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): CreateAccountRequest;
  static deserializeBinaryFromReader(message: CreateAccountRequest, reader: jspb.BinaryReader): CreateAccountRequest;
}

export namespace CreateAccountRequest {
  export type AsObject = {
    accountId: string,
    accountName: string,
    contactPerson: string,
    currency: string,
    createdBy: string,
  }
}

export class GetAccountRequest extends jspb.Message {
  getAccountId(): string;
  setAccountId(value: string): GetAccountRequest;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): GetAccountRequest.AsObject;
  static toObject(includeInstance: boolean, msg: GetAccountRequest): GetAccountRequest.AsObject;
  static serializeBinaryToWriter(message: GetAccountRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): GetAccountRequest;
  static deserializeBinaryFromReader(message: GetAccountRequest, reader: jspb.BinaryReader): GetAccountRequest;
}

export namespace GetAccountRequest {
  export type AsObject = {
    accountId: string,
  }
}

export class ListAccountsRequest extends jspb.Message {
  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): ListAccountsRequest.AsObject;
  static toObject(includeInstance: boolean, msg: ListAccountsRequest): ListAccountsRequest.AsObject;
  static serializeBinaryToWriter(message: ListAccountsRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): ListAccountsRequest;
  static deserializeBinaryFromReader(message: ListAccountsRequest, reader: jspb.BinaryReader): ListAccountsRequest;
}

export namespace ListAccountsRequest {
  export type AsObject = {
  }
}

export class ListAccountsResponse extends jspb.Message {
  getAccountsList(): Array<Account>;
  setAccountsList(value: Array<Account>): ListAccountsResponse;
  clearAccountsList(): ListAccountsResponse;
  addAccounts(value?: Account, index?: number): Account;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): ListAccountsResponse.AsObject;
  static toObject(includeInstance: boolean, msg: ListAccountsResponse): ListAccountsResponse.AsObject;
  static serializeBinaryToWriter(message: ListAccountsResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): ListAccountsResponse;
  static deserializeBinaryFromReader(message: ListAccountsResponse, reader: jspb.BinaryReader): ListAccountsResponse;
}

export namespace ListAccountsResponse {
  export type AsObject = {
    accountsList: Array<Account.AsObject>,
  }
}

