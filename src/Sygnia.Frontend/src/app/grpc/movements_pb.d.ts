import * as jspb from 'google-protobuf'

import * as google_protobuf_timestamp_pb from 'google-protobuf/google/protobuf/timestamp_pb'; // proto import: "google/protobuf/timestamp.proto"


export class Movement extends jspb.Message {
  getAccountId(): string;
  setAccountId(value: string): Movement;

  getExternalRef(): string;
  setExternalRef(value: string): Movement;

  getCurrency(): string;
  setCurrency(value: string): Movement;

  getAmount(): string;
  setAmount(value: string): Movement;

  getOccurredAt(): google_protobuf_timestamp_pb.Timestamp | undefined;
  setOccurredAt(value?: google_protobuf_timestamp_pb.Timestamp): Movement;
  hasOccurredAt(): boolean;
  clearOccurredAt(): Movement;

  getNarration(): string;
  setNarration(value: string): Movement;

  getRefNr(): string;
  setRefNr(value: string): Movement;

  getMovedBy(): string;
  setMovedBy(value: string): Movement;

  getMovedDate(): google_protobuf_timestamp_pb.Timestamp | undefined;
  setMovedDate(value?: google_protobuf_timestamp_pb.Timestamp): Movement;
  hasMovedDate(): boolean;
  clearMovedDate(): Movement;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): Movement.AsObject;
  static toObject(includeInstance: boolean, msg: Movement): Movement.AsObject;
  static serializeBinaryToWriter(message: Movement, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): Movement;
  static deserializeBinaryFromReader(message: Movement, reader: jspb.BinaryReader): Movement;
}

export namespace Movement {
  export type AsObject = {
    accountId: string,
    externalRef: string,
    currency: string,
    amount: string,
    occurredAt?: google_protobuf_timestamp_pb.Timestamp.AsObject,
    narration: string,
    refNr: string,
    movedBy: string,
    movedDate?: google_protobuf_timestamp_pb.Timestamp.AsObject,
  }
}

export class SubmitMovementRequest extends jspb.Message {
  getAccountId(): string;
  setAccountId(value: string): SubmitMovementRequest;

  getExternalRef(): string;
  setExternalRef(value: string): SubmitMovementRequest;

  getCurrency(): string;
  setCurrency(value: string): SubmitMovementRequest;

  getAmount(): string;
  setAmount(value: string): SubmitMovementRequest;

  getOccurredAt(): google_protobuf_timestamp_pb.Timestamp | undefined;
  setOccurredAt(value?: google_protobuf_timestamp_pb.Timestamp): SubmitMovementRequest;
  hasOccurredAt(): boolean;
  clearOccurredAt(): SubmitMovementRequest;

  getNarration(): string;
  setNarration(value: string): SubmitMovementRequest;

  getRefNr(): string;
  setRefNr(value: string): SubmitMovementRequest;

  getMovedBy(): string;
  setMovedBy(value: string): SubmitMovementRequest;

  getMovedDate(): google_protobuf_timestamp_pb.Timestamp | undefined;
  setMovedDate(value?: google_protobuf_timestamp_pb.Timestamp): SubmitMovementRequest;
  hasMovedDate(): boolean;
  clearMovedDate(): SubmitMovementRequest;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): SubmitMovementRequest.AsObject;
  static toObject(includeInstance: boolean, msg: SubmitMovementRequest): SubmitMovementRequest.AsObject;
  static serializeBinaryToWriter(message: SubmitMovementRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): SubmitMovementRequest;
  static deserializeBinaryFromReader(message: SubmitMovementRequest, reader: jspb.BinaryReader): SubmitMovementRequest;
}

export namespace SubmitMovementRequest {
  export type AsObject = {
    accountId: string,
    externalRef: string,
    currency: string,
    amount: string,
    occurredAt?: google_protobuf_timestamp_pb.Timestamp.AsObject,
    narration: string,
    refNr: string,
    movedBy: string,
    movedDate?: google_protobuf_timestamp_pb.Timestamp.AsObject,
  }
}

export class TransferRequest extends jspb.Message {
  getFromAccountId(): string;
  setFromAccountId(value: string): TransferRequest;

  getToAccountId(): string;
  setToAccountId(value: string): TransferRequest;

  getExternalRef(): string;
  setExternalRef(value: string): TransferRequest;

  getCurrency(): string;
  setCurrency(value: string): TransferRequest;

  getAmount(): string;
  setAmount(value: string): TransferRequest;

  getOccurredAt(): google_protobuf_timestamp_pb.Timestamp | undefined;
  setOccurredAt(value?: google_protobuf_timestamp_pb.Timestamp): TransferRequest;
  hasOccurredAt(): boolean;
  clearOccurredAt(): TransferRequest;

  getNarration(): string;
  setNarration(value: string): TransferRequest;

  getRefNr(): string;
  setRefNr(value: string): TransferRequest;

  getMovedBy(): string;
  setMovedBy(value: string): TransferRequest;

  getMovedDate(): google_protobuf_timestamp_pb.Timestamp | undefined;
  setMovedDate(value?: google_protobuf_timestamp_pb.Timestamp): TransferRequest;
  hasMovedDate(): boolean;
  clearMovedDate(): TransferRequest;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): TransferRequest.AsObject;
  static toObject(includeInstance: boolean, msg: TransferRequest): TransferRequest.AsObject;
  static serializeBinaryToWriter(message: TransferRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): TransferRequest;
  static deserializeBinaryFromReader(message: TransferRequest, reader: jspb.BinaryReader): TransferRequest;
}

export namespace TransferRequest {
  export type AsObject = {
    fromAccountId: string,
    toAccountId: string,
    externalRef: string,
    currency: string,
    amount: string,
    occurredAt?: google_protobuf_timestamp_pb.Timestamp.AsObject,
    narration: string,
    refNr: string,
    movedBy: string,
    movedDate?: google_protobuf_timestamp_pb.Timestamp.AsObject,
  }
}

export class TransferResponse extends jspb.Message {
  getDebit(): Movement | undefined;
  setDebit(value?: Movement): TransferResponse;
  hasDebit(): boolean;
  clearDebit(): TransferResponse;

  getCredit(): Movement | undefined;
  setCredit(value?: Movement): TransferResponse;
  hasCredit(): boolean;
  clearCredit(): TransferResponse;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): TransferResponse.AsObject;
  static toObject(includeInstance: boolean, msg: TransferResponse): TransferResponse.AsObject;
  static serializeBinaryToWriter(message: TransferResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): TransferResponse;
  static deserializeBinaryFromReader(message: TransferResponse, reader: jspb.BinaryReader): TransferResponse;
}

export namespace TransferResponse {
  export type AsObject = {
    debit?: Movement.AsObject,
    credit?: Movement.AsObject,
  }
}

export class GetBalanceRequest extends jspb.Message {
  getAccountId(): string;
  setAccountId(value: string): GetBalanceRequest;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): GetBalanceRequest.AsObject;
  static toObject(includeInstance: boolean, msg: GetBalanceRequest): GetBalanceRequest.AsObject;
  static serializeBinaryToWriter(message: GetBalanceRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): GetBalanceRequest;
  static deserializeBinaryFromReader(message: GetBalanceRequest, reader: jspb.BinaryReader): GetBalanceRequest;
}

export namespace GetBalanceRequest {
  export type AsObject = {
    accountId: string,
  }
}

export class GetBalanceResponse extends jspb.Message {
  getAccountId(): string;
  setAccountId(value: string): GetBalanceResponse;

  getBalance(): string;
  setBalance(value: string): GetBalanceResponse;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): GetBalanceResponse.AsObject;
  static toObject(includeInstance: boolean, msg: GetBalanceResponse): GetBalanceResponse.AsObject;
  static serializeBinaryToWriter(message: GetBalanceResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): GetBalanceResponse;
  static deserializeBinaryFromReader(message: GetBalanceResponse, reader: jspb.BinaryReader): GetBalanceResponse;
}

export namespace GetBalanceResponse {
  export type AsObject = {
    accountId: string,
    balance: string,
  }
}

export class GetStatementRequest extends jspb.Message {
  getAccountId(): string;
  setAccountId(value: string): GetStatementRequest;

  getFrom(): google_protobuf_timestamp_pb.Timestamp | undefined;
  setFrom(value?: google_protobuf_timestamp_pb.Timestamp): GetStatementRequest;
  hasFrom(): boolean;
  clearFrom(): GetStatementRequest;

  getTo(): google_protobuf_timestamp_pb.Timestamp | undefined;
  setTo(value?: google_protobuf_timestamp_pb.Timestamp): GetStatementRequest;
  hasTo(): boolean;
  clearTo(): GetStatementRequest;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): GetStatementRequest.AsObject;
  static toObject(includeInstance: boolean, msg: GetStatementRequest): GetStatementRequest.AsObject;
  static serializeBinaryToWriter(message: GetStatementRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): GetStatementRequest;
  static deserializeBinaryFromReader(message: GetStatementRequest, reader: jspb.BinaryReader): GetStatementRequest;
}

export namespace GetStatementRequest {
  export type AsObject = {
    accountId: string,
    from?: google_protobuf_timestamp_pb.Timestamp.AsObject,
    to?: google_protobuf_timestamp_pb.Timestamp.AsObject,
  }
}

export class StatementLine extends jspb.Message {
  getMovement(): Movement | undefined;
  setMovement(value?: Movement): StatementLine;
  hasMovement(): boolean;
  clearMovement(): StatementLine;

  getRunningTotal(): string;
  setRunningTotal(value: string): StatementLine;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): StatementLine.AsObject;
  static toObject(includeInstance: boolean, msg: StatementLine): StatementLine.AsObject;
  static serializeBinaryToWriter(message: StatementLine, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): StatementLine;
  static deserializeBinaryFromReader(message: StatementLine, reader: jspb.BinaryReader): StatementLine;
}

export namespace StatementLine {
  export type AsObject = {
    movement?: Movement.AsObject,
    runningTotal: string,
  }
}

export class GetStatementPageRequest extends jspb.Message {
  getAccountId(): string;
  setAccountId(value: string): GetStatementPageRequest;

  getFrom(): google_protobuf_timestamp_pb.Timestamp | undefined;
  setFrom(value?: google_protobuf_timestamp_pb.Timestamp): GetStatementPageRequest;
  hasFrom(): boolean;
  clearFrom(): GetStatementPageRequest;

  getTo(): google_protobuf_timestamp_pb.Timestamp | undefined;
  setTo(value?: google_protobuf_timestamp_pb.Timestamp): GetStatementPageRequest;
  hasTo(): boolean;
  clearTo(): GetStatementPageRequest;

  getPageNumber(): number;
  setPageNumber(value: number): GetStatementPageRequest;

  getPageSize(): number;
  setPageSize(value: number): GetStatementPageRequest;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): GetStatementPageRequest.AsObject;
  static toObject(includeInstance: boolean, msg: GetStatementPageRequest): GetStatementPageRequest.AsObject;
  static serializeBinaryToWriter(message: GetStatementPageRequest, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): GetStatementPageRequest;
  static deserializeBinaryFromReader(message: GetStatementPageRequest, reader: jspb.BinaryReader): GetStatementPageRequest;
}

export namespace GetStatementPageRequest {
  export type AsObject = {
    accountId: string,
    from?: google_protobuf_timestamp_pb.Timestamp.AsObject,
    to?: google_protobuf_timestamp_pb.Timestamp.AsObject,
    pageNumber: number,
    pageSize: number,
  }
}

export class GetStatementPageResponse extends jspb.Message {
  getLinesList(): Array<StatementLine>;
  setLinesList(value: Array<StatementLine>): GetStatementPageResponse;
  clearLinesList(): GetStatementPageResponse;
  addLines(value?: StatementLine, index?: number): StatementLine;

  getTotalCount(): number;
  setTotalCount(value: number): GetStatementPageResponse;

  serializeBinary(): Uint8Array;
  toObject(includeInstance?: boolean): GetStatementPageResponse.AsObject;
  static toObject(includeInstance: boolean, msg: GetStatementPageResponse): GetStatementPageResponse.AsObject;
  static serializeBinaryToWriter(message: GetStatementPageResponse, writer: jspb.BinaryWriter): void;
  static deserializeBinary(bytes: Uint8Array): GetStatementPageResponse;
  static deserializeBinaryFromReader(message: GetStatementPageResponse, reader: jspb.BinaryReader): GetStatementPageResponse;
}

export namespace GetStatementPageResponse {
  export type AsObject = {
    linesList: Array<StatementLine.AsObject>,
    totalCount: number,
  }
}

