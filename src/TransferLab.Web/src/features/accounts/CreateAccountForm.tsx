import { useState, type FormEvent } from "react";
import type { CreateAccountInput } from "../../api/types";
import { getErrorMessage } from "../../api/client";

interface CreateAccountFormProps {
  onCreate: (input: CreateAccountInput) => Promise<void>;
}

export function CreateAccountForm({ onCreate }: CreateAccountFormProps) {
  const [name, setName] = useState("");
  const [openingBalance, setOpeningBalance] = useState("1000.00");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    const balance = Number(openingBalance);
    if (!name.trim() || !Number.isFinite(balance) || balance < 0) {
      setError("Enter an account name and a non-negative opening balance.");
      return;
    }

    try {
      setIsSubmitting(true);
      await onCreate({ name: name.trim(), openingBalance: balance });
      setName("");
      setOpeningBalance("1000.00");
    } catch (submitError) {
      setError(getErrorMessage(submitError));
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form className="panel form-panel" onSubmit={handleSubmit}>
      <div className="panel-heading">
        <div>
          <p className="eyebrow">New resource</p>
          <h2>Create an account</h2>
        </div>
        <span className="step-label">01</span>
      </div>

      <label className="field">
        <span>Account name</span>
        <input
          name="accountName"
          type="text"
          value={name}
          onChange={(event) => setName(event.target.value)}
          placeholder="Operating account"
          maxLength={200}
          autoComplete="off"
        />
      </label>

      <label className="field">
        <span>Opening balance</span>
        <div className="money-input">
          <span aria-hidden="true">$</span>
          <input
            name="openingBalance"
            type="number"
            value={openingBalance}
            onChange={(event) => setOpeningBalance(event.target.value)}
            min="0"
            step="0.01"
            inputMode="decimal"
          />
        </div>
      </label>

      {error ? (
        <p className="form-message message-error" role="alert">
          {error}
        </p>
      ) : (
        <p className="form-hint">
          Accounts created here stay indexed in this browser.
        </p>
      )}

      <button className="button button-primary button-full" disabled={isSubmitting}>
        <span>{isSubmitting ? "Creating…" : "Create account"}</span>
        <span aria-hidden="true">→</span>
      </button>
    </form>
  );
}
