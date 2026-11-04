import { useEffect, useMemo, useState, type FormEvent } from "react";
import { getErrorMessage } from "../../api/client";
import type { Account, CreateTransferInput } from "../../api/types";
import { formatMoney } from "../../lib/format";

interface CreateTransferFormProps {
  accounts: Account[];
  onCreate: (input: CreateTransferInput) => Promise<void>;
}

export function CreateTransferForm({
  accounts,
  onCreate,
}: CreateTransferFormProps) {
  const [sourceAccountId, setSourceAccountId] = useState("");
  const [destinationAccountId, setDestinationAccountId] = useState("");
  const [amount, setAmount] = useState("25.00");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const sourceExists = accounts.some(
      (account) => account.id === sourceAccountId,
    );
    const destinationExists = accounts.some(
      (account) => account.id === destinationAccountId,
    );

    if (!sourceExists) setSourceAccountId(accounts[0]?.id ?? "");
    if (!destinationExists || destinationAccountId === sourceAccountId) {
      setDestinationAccountId(
        accounts.find((account) => account.id !== sourceAccountId)?.id ?? "",
      );
    }
  }, [accounts, destinationAccountId, sourceAccountId]);

  const selectedSource = useMemo(
    () => accounts.find((account) => account.id === sourceAccountId),
    [accounts, sourceAccountId],
  );

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    const numericAmount = Number(amount);
    if (
      !sourceAccountId ||
      !destinationAccountId ||
      sourceAccountId === destinationAccountId ||
      !Number.isFinite(numericAmount) ||
      numericAmount <= 0
    ) {
      setError("Choose two different accounts and enter a positive amount.");
      return;
    }

    try {
      setIsSubmitting(true);
      await onCreate({
        sourceAccountId,
        destinationAccountId,
        amount: numericAmount,
      });
      setAmount("25.00");
    } catch (submitError) {
      setError(getErrorMessage(submitError));
    } finally {
      setIsSubmitting(false);
    }
  }

  const canTransfer = accounts.length >= 2;

  return (
    <form className="panel form-panel transfer-form" onSubmit={handleSubmit}>
      <div className="panel-heading">
        <div>
          <p className="eyebrow">New operation</p>
          <h2>Queue a transfer</h2>
        </div>
        <span className="step-label">02</span>
      </div>

      {!canTransfer ? (
        <div className="form-prerequisite">
          <strong>Two accounts required</strong>
          <p>Create at least two accounts before submitting a transfer.</p>
        </div>
      ) : (
        <>
          <label className="field">
            <span>From</span>
            <select
              value={sourceAccountId}
              onChange={(event) => setSourceAccountId(event.target.value)}
            >
              {accounts.map((account) => (
                <option key={account.id} value={account.id}>
                  {account.name} · {formatMoney(account.balance)}
                </option>
              ))}
            </select>
          </label>

          <div className="transfer-direction" aria-hidden="true">
            <span>↓</span>
          </div>

          <label className="field">
            <span>To</span>
            <select
              value={destinationAccountId}
              onChange={(event) => setDestinationAccountId(event.target.value)}
            >
              {accounts
                .filter((account) => account.id !== sourceAccountId)
                .map((account) => (
                  <option key={account.id} value={account.id}>
                    {account.name} · {formatMoney(account.balance)}
                  </option>
                ))}
            </select>
          </label>

          <label className="field">
            <span>Amount</span>
            <div className="money-input">
              <span aria-hidden="true">$</span>
              <input
                type="number"
                value={amount}
                onChange={(event) => setAmount(event.target.value)}
                min="0.01"
                step="0.01"
                inputMode="decimal"
              />
            </div>
          </label>

          <p className="form-hint">
            {selectedSource
              ? `${formatMoney(selectedSource.balance)} currently available.`
              : "Select a source account."}
          </p>
        </>
      )}

      {error && (
        <p className="form-message message-error" role="alert">
          {error}
        </p>
      )}

      <button
        className="button button-primary button-full"
        disabled={!canTransfer || isSubmitting}
      >
        <span>{isSubmitting ? "Submitting…" : "Submit transfer"}</span>
        <span aria-hidden="true">→</span>
      </button>
    </form>
  );
}
