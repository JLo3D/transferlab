import type {
  Account,
  CreateAccountInput,
  CreatedResource,
  CreateTransferInput,
  ProblemDetails,
  Transfer,
} from "./types";

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/$/, "");

export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly problem?: ProblemDetails,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      ...init?.headers,
    },
  });

  if (!response.ok) {
    let problem: ProblemDetails | undefined;

    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      problem = undefined;
    }

    throw new ApiError(
      problem?.detail ?? problem?.title ?? `Request failed (${response.status}).`,
      response.status,
      problem,
    );
  }

  return (await response.json()) as T;
}

export const transferLabApi = {
  createAccount(input: CreateAccountInput) {
    return request<CreatedResource>("/api/accounts/", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(input),
    });
  },

  getAccount(id: string) {
    return request<Account>(`/api/accounts/${id}`);
  },

  createTransfer(input: CreateTransferInput, idempotencyKey: string) {
    return request<CreatedResource>("/api/transfers/", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Idempotency-Key": idempotencyKey,
      },
      body: JSON.stringify(input),
    });
  },

  getTransfer(id: string) {
    return request<Transfer>(`/api/transfers/${id}`);
  },
};

export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message;
  if (error instanceof TypeError) {
    return "The API could not be reached. Start TransferLab.Api and try again.";
  }
  if (error instanceof Error) return error.message;

  return "Something unexpected happened. Please try again.";
}
