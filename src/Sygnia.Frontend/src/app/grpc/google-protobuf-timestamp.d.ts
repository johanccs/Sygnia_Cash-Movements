// google-protobuf ships no .d.ts for this submodule (only movements_pb.d.ts references it,
// and skipLibCheck lets that slide). Regular .ts files importing it directly need this
// minimal ambient declaration so the compiler can type-check Timestamp usage.
declare module 'google-protobuf/google/protobuf/timestamp_pb' {
  import * as jspb from 'google-protobuf';

  export class Timestamp extends jspb.Message {
    static fromDate(date: Date): Timestamp;
    toDate(): Date;
    getSeconds(): number;
    setSeconds(value: number): Timestamp;
    getNanos(): number;
    setNanos(value: number): Timestamp;
  }
}
