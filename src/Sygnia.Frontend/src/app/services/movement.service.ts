import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { Timestamp } from 'google-protobuf/google/protobuf/timestamp_pb';
import { MovementServiceClient } from '../grpc/MovementsServiceClientPb';
import {
  GetBalanceRequest,
  GetStatementPageRequest,
  Movement,
  StatementLine,
  SubmitMovementRequest,
  TransferRequest,
} from '../grpc/movements_pb';

export interface BalanceDto {
  accountId: string;
  balance: string;
}

/** Plain DTO mirroring the wire Movement message, with Timestamps converted to JS Dates. */
export interface MovementDto {
  accountId: string;
  externalRef: string;
  currency: string;
  amount: string;
  occurredAt: Date | null;
  narration: string;
  refNr: string;
  movedBy: string;
  movedDate: Date | null;
}

export interface SubmitMovementInput {
  accountId: string;
  externalRef: string;
  currency: string;
  amount: string;
  occurredAt: Date;
  narration: string;
  refNr: string;
  movedBy: string;
  movedDate: Date;
}

export interface TransferInput {
  fromAccountId: string;
  toAccountId: string;
  externalRef: string;
  currency: string;
  amount: string;
  occurredAt: Date;
  narration: string;
  refNr: string;
  movedBy: string;
  movedDate: Date;
}

export interface TransferResultDto {
  debit: MovementDto | null;
  credit: MovementDto | null;
}

/**
 * runningTotal is null for pages sourced from GetStatementPage: a running total isn't
 * well-defined for an isolated page. Only the full-stream GetStatement (not wrapped by
 * this service) produces a real, meaningful runningTotal per line.
 */
export interface StatementLineDto {
  movement: MovementDto | null;
  runningTotal: string | null;
}

export interface StatementPageDto {
  lines: StatementLineDto[];
  totalCount: number;
}

export interface GetStatementPageInput {
  accountId: string;
  from?: Date;
  to?: Date;
  pageNumber: number;
  pageSize: number;
}

function toTimestamp(date: Date): Timestamp {
  return Timestamp.fromDate(date);
}

function mapMovement(movement: Movement | undefined): MovementDto | null {
  if (!movement) {
    return null;
  }
  return {
    accountId: movement.getAccountId(),
    externalRef: movement.getExternalRef(),
    currency: movement.getCurrency(),
    amount: movement.getAmount(),
    occurredAt: movement.getOccurredAt()?.toDate() ?? null,
    narration: movement.getNarration(),
    refNr: movement.getRefNr(),
    movedBy: movement.getMovedBy(),
    movedDate: movement.getMovedDate()?.toDate() ?? null,
  };
}

function mapStatementLine(line: StatementLine): StatementLineDto {
  return {
    movement: mapMovement(line.getMovement()),
    // See StatementLineDto doc comment: paged results leave running_total unset on the wire.
    runningTotal: line.getRunningTotal() || null,
  };
}

@Injectable({
  providedIn: 'root',
  useFactory: () => new MovementService(new MovementServiceClient('http://localhost:5000')),
})
export class MovementService {
  constructor(private readonly client: MovementServiceClient) {}

  submitMovement(input: SubmitMovementInput): Observable<MovementDto | null> {
    return new Observable(observer => {
      const req = new SubmitMovementRequest();
      req.setAccountId(input.accountId);
      req.setExternalRef(input.externalRef);
      req.setCurrency(input.currency);
      req.setAmount(input.amount);
      req.setOccurredAt(toTimestamp(input.occurredAt));
      req.setNarration(input.narration);
      req.setRefNr(input.refNr);
      req.setMovedBy(input.movedBy);
      req.setMovedDate(toTimestamp(input.movedDate));
      this.client.submitMovement(req, {}, (err, res) => {
        if (err) {
          observer.error(err);
          return;
        }
        observer.next(mapMovement(res));
        observer.complete();
      });
    });
  }

  transfer(input: TransferInput): Observable<TransferResultDto> {
    return new Observable(observer => {
      const req = new TransferRequest();
      req.setFromAccountId(input.fromAccountId);
      req.setToAccountId(input.toAccountId);
      req.setExternalRef(input.externalRef);
      req.setCurrency(input.currency);
      req.setAmount(input.amount);
      req.setOccurredAt(toTimestamp(input.occurredAt));
      req.setNarration(input.narration);
      req.setRefNr(input.refNr);
      req.setMovedBy(input.movedBy);
      req.setMovedDate(toTimestamp(input.movedDate));
      this.client.transfer(req, {}, (err, res) => {
        if (err) {
          observer.error(err);
          return;
        }
        observer.next({
          debit: mapMovement(res.getDebit()),
          credit: mapMovement(res.getCredit()),
        });
        observer.complete();
      });
    });
  }

  getBalance(accountId: string): Observable<BalanceDto> {
    return new Observable(observer => {
      const req = new GetBalanceRequest();
      req.setAccountId(accountId);
      this.client.getBalance(req, {}, (err, res) => {
        if (err) {
          observer.error(err);
          return;
        }
        observer.next({ accountId: res.getAccountId(), balance: res.getBalance() });
        observer.complete();
      });
    });
  }

  getStatementPage(input: GetStatementPageInput): Observable<StatementPageDto> {
    return new Observable(observer => {
      const req = new GetStatementPageRequest();
      req.setAccountId(input.accountId);
      if (input.from) {
        req.setFrom(toTimestamp(input.from));
      }
      if (input.to) {
        req.setTo(toTimestamp(input.to));
      }
      req.setPageNumber(input.pageNumber);
      req.setPageSize(input.pageSize);
      this.client.getStatementPage(req, {}, (err, res) => {
        if (err) {
          observer.error(err);
          return;
        }
        observer.next({
          lines: res.getLinesList().map(mapStatementLine),
          totalCount: res.getTotalCount(),
        });
        observer.complete();
      });
    });
  }
}
