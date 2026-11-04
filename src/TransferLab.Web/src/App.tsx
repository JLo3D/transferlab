import { useCallback, useEffect, useMemo, useState } from "react";
import type {
  Account,
  CreateAccountInput,
  CreateTransferInput,
  Transfer,
} from "./api/types";
import { getErrorMessage, transferLabApi } from "./api/client";
import { AccountCard } from "./components/AccountCard";
import { EmptyState } from "./components/EmptyState";
import { MetricCard } from "./components/MetricCard";
import { CreateAccountForm } from "./features/accounts/CreateAccountForm";
import { CreateTransferForm } from "./features/transfers/CreateTransferForm";
import { TransferActivity } from "./features/transfers/TransferActivity";
import { formatMoney } from "./lib/format";

type ApiState = "unverified" | "online" | "offline";

function sortTransfers(transfers: Transfer[]): Transfer[] {
  return [...transfers].sort(
    (left, right) =>
      new Date(right.createdUtc).getTime() -
      new Date(left.createdUtc).getTime(),
  );
}

export default function App() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [transfers, setTransfers] = useState<Transfer[]>([]);
  const [apiState, setApiState] = useState<ApiState>("unverified");
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [refreshError, setRefreshError] = useState<string | null>(null);

  const refreshAccounts = useCallback(async () => {
    const nextAccounts = await transferLabApi.listAccounts();
    setAccounts(nextAccounts);
  }, []);

  const refreshTransfers = useCallback(async () => {
    const nextTransfers = await transferLabApi.listTransfers();
    setTransfers(sortTransfers(nextTransfers));
  }, []);

  const refreshWorkspace = useCallback(async () => {
    try {
      setIsRefreshing(true);
      setRefreshError(null);
      await Promise.all([refreshAccounts(), refreshTransfers()]);
      setApiState("online");
    } catch (error) {
      setApiState("offline");
      setRefreshError(getErrorMessage(error));
    } finally {
      setIsRefreshing(false);
    }
  }, [refreshAccounts, refreshTransfers]);

  useEffect(() => {
    void refreshWorkspace();
  }, [refreshWorkspace]);

  const hasActiveTransfer = transfers.some(
    (transfer) =>
      transfer.status === "Pending" || transfer.status === "Processing",
  );

  useEffect(() => {
    if (!hasActiveTransfer) return;

    const timer = window.setInterval(() => {
      void (async () => {
        try {
          await refreshTransfers();
          await refreshAccounts();
          setApiState("online");
          setRefreshError(null);
        } catch (error) {
          setApiState("offline");
          setRefreshError(getErrorMessage(error));
        }
      })();
    }, 2000);

    return () => window.clearInterval(timer);
  }, [hasActiveTransfer, refreshAccounts, refreshTransfers]);

  async function handleCreateAccount(input: CreateAccountInput) {
    await transferLabApi.createAccount(input);
    await refreshAccounts();
    setApiState("online");
    setRefreshError(null);
  }

  async function handleCreateTransfer(input: CreateTransferInput) {
    const idempotencyKey = `web-${crypto.randomUUID()}`;
    await transferLabApi.createTransfer(input, idempotencyKey);
    await Promise.all([refreshTransfers(), refreshAccounts()]);
    setApiState("online");
    setRefreshError(null);
  }

  const totalBalance = useMemo(
    () => accounts.reduce((total, account) => total + account.balance, 0),
    [accounts],
  );
  const activeCount = transfers.filter(
    (transfer) =>
      transfer.status === "Pending" || transfer.status === "Processing",
  ).length;
  const completedCount = transfers.filter(
    (transfer) => transfer.status === "Completed",
  ).length;

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <a className="brand" href="#overview" aria-label="TransferLab home">
          <span className="brand-mark" aria-hidden="true">
            TL
          </span>
          <span>
            <strong>TransferLab</strong>
            <small>Operations workspace</small>
          </span>
        </a>

        <nav className="primary-nav" aria-label="Primary navigation">
          <a className="nav-link nav-active" href="#overview">
            <span>01</span>
            Overview
          </a>
          <a className="nav-link" href="#accounts">
            <span>02</span>
            Accounts
          </a>
          <a className="nav-link" href="#transfers">
            <span>03</span>
            New transfer
          </a>
          <a className="nav-link" href="#activity">
            <span>04</span>
            Activity
          </a>
        </nav>

        <div className="sidebar-footer">
          <div>
            <span className={`connection-dot connection-${apiState}`} />
            <span>
              <strong>
                {apiState === "online"
                  ? "API connected"
                  : apiState === "offline"
                    ? "API unavailable"
                    : "API not checked"}
              </strong>
              <small>peepi Kubernetes</small>
            </span>
          </div>
        </div>
      </aside>

      <main className="main-content" id="overview">
        <header className="page-header">
          <div>
            <p className="eyebrow">System overview</p>
            <h1>Transfer operations</h1>
            <p>
              Create resources, queue work, and watch the processing model run.
            </p>
          </div>
          <button
            className="button button-secondary"
            onClick={() => void refreshWorkspace()}
            disabled={isRefreshing}
          >
            <span className={isRefreshing ? "refresh-icon spinning" : "refresh-icon"}>
              ↻
            </span>
            {isRefreshing ? "Refreshing" : "Refresh data"}
          </button>
        </header>

        {refreshError && (
          <div className="notice notice-error" role="alert">
            <strong>Workspace could not refresh.</strong>
            <span>{refreshError}</span>
          </div>
        )}

        <section className="metrics-grid" aria-label="Workspace summary">
          <MetricCard
            label="Accounts"
            value={accounts.length.toString().padStart(2, "0")}
            detail="Stored in the database"
          />
          <MetricCard
            label="Combined balance"
            value={formatMoney(totalBalance)}
            detail="Across all accounts"
            tone="accent"
          />
          <MetricCard
            label="Active transfers"
            value={activeCount.toString().padStart(2, "0")}
            detail="Pending or processing"
          />
          <MetricCard
            label="Completed"
            value={completedCount.toString().padStart(2, "0")}
            detail="Recent transfer activity"
            tone="positive"
          />
        </section>

        <section className="workspace-grid" id="accounts">
          <div className="panel accounts-panel">
            <div className="panel-heading">
              <div>
                <p className="eyebrow">Resources</p>
                <h2>Accounts</h2>
              </div>
              <span className="panel-count">{accounts.length}</span>
            </div>

            {accounts.length === 0 ? (
              <EmptyState
                title="No accounts yet"
                detail="Create two accounts to unlock the transfer workflow."
              />
            ) : (
              <div className="account-list">
                {accounts.map((account, index) => (
                  <AccountCard
                    account={account}
                    index={index}
                    key={account.id}
                  />
                ))}
              </div>
            )}
          </div>

          <CreateAccountForm onCreate={handleCreateAccount} />
        </section>

        <section className="operations-grid" id="transfers">
          <CreateTransferForm
            accounts={accounts}
            onCreate={handleCreateTransfer}
          />
          <TransferActivity transfers={transfers} accounts={accounts} />
        </section>
      </main>
    </div>
  );
}
