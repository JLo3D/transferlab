export interface Account {
  id: string;
  name: string;
  balance: number;
}

export type TransferStatus =
  | "Pending"
  | "Processing"
  | "Completed"
  | "Failed";

export interface Transfer {
  id: string;
  sourceAccountId: string;
  destinationAccountId: string;
  amount: number;
  status: TransferStatus;
  createdUtc: string;
  processingStartedUtc: string | null;
  attemptCount: number;
}

export interface CreateAccountInput {
  name: string;
  openingBalance: number;
}

export interface CreateTransferInput {
  sourceAccountId: string;
  destinationAccountId: string;
  amount: number;
}

export interface CreatedResource {
  id: string;
}

export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  instance?: string;
  traceId?: string;
}
