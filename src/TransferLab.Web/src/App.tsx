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
import {
  loadWorkspace,
  registerAccount,
  registerTransfer,
  saveWorkspace,
  type WorkspaceIndex,
} from "./lib/workspace";

type ApiState = "unverified" | "online" | "offline";

function sortTransfers(transfers: Transfer[]): Transfer[] {
  return [...transfers].sort(
    (left, right) =>
      new Date(right.createdUtc).getTime() -
      new Date(left.createdUtc).getTime(),
  );
}

export default function App() {
  const [workspace, setWorkspace] = useState<WorkspaceIndex>(loadWorkspace);
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [transfers, setTransfers] = useState<Transfer[]>([]);
  const [apiState, setApiState] = useState<ApiState>("unverified");
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [refreshError, setRefreshError] = useState<string | null>(null);

  useEffect(() => saveWorkspace(workspace), [workspace]);

  const refreshAccounts = useCallback(async () => {
    if (workspace.accountIds.length === 0) {
      setAccounts([]);
      return;
    }

    const nextAccounts = await Promise.all(
      workspace.accountIds.map((id) => transferLabApi.getAccount(id)),
    );
    setAccounts(nextAccounts);
  }, [workspace.accountIds]);

  const refreshTransfers = useCallback(async () => {
    if (workspace.transferIds.length === 0) {
      setTransfers([]);
      return;
    }

    const nextTransfers = await Promise.all(
      workspace.transferIds.map((id) => transferLabApi.getTransfer(id)),
    );
    setTransfers(sortTransfers(nextTransfers));
  }, [workspace.transferIds]);

  const refreshWorkspace = useCallback(async () => {
    if (
      workspace.accountIds.length === 0 &&
      workspace.transferIds.length === 0
    ) {
      return;
    }

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
  }, [refreshAccounts, refreshTransfers, workspace]);

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
    const created = await transferLabApi.createAccount(input);
    setWorkspace((current) => registerAccount(current, created.id));

    const account = await transferLabApi.getAccount(created.id);
    setAccounts((current) => [
      ...current.filter((item) => item.id !== account.id),
      account,
    ]);
    setApiState("online");
    setRefreshError(null);
  }

  async function handleCreateTransfer(input: CreateTransferInput) {
    const idempotencyKey = `web-${crypto.randomUUID()}`;
    const created = await transferLabApi.createTransfer(input, idempotencyKey);
    setWorkspace((current) => registerTransfer(current, created.id));

    const transfer = await transferLabApi.getTransfer(created.id);
    setTransfers((current) =>
      sortTransfers([
        transfer,
        ...current.filter((item) => item.id !== transfer.id),
      ]),
    );
    setApiState("online");
    setRefreshError(null);
  }

  function clearLocalWorkspace() {
    setWorkspace({ accountIds: [], transferIds: [] });
    setAccounts([]);
    setTransfers([]);
    setApiState("unverified");
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
              <small>Local environment</small>
            </span>
          </div>
          <button className="text-button" onClick={clearLocalWorkspace}>
            Clear local view
          </button>
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
            label="Known accounts"
            value={accounts.length.toString().padStart(2, "0")}
            detail="Indexed in this browser"
          />
          <MetricCard
            label="Combined balance"
            value={formatMoney(totalBalance)}
            detail="Across known accounts"
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
            detail="In this workspace"
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
                title="Start with an account"
                detail="Create two accounts to unlock the full transfer workflow."
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
