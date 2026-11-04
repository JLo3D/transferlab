import type { Account, Transfer } from "../../api/types";
import { EmptyState } from "../../components/EmptyState";
import { StatusBadge } from "../../components/StatusBadge";
import { formatDate, formatMoney, shortId } from "../../lib/format";

interface TransferActivityProps {
  transfers: Transfer[];
  accounts: Account[];
}

export function TransferActivity({
  transfers,
  accounts,
}: TransferActivityProps) {
  const accountNames = new Map(
    accounts.map((account) => [account.id, account.name]),
  );

  return (
    <section className="panel activity-panel" id="activity">
      <div className="panel-heading">
        <div>
          <p className="eyebrow">Live processing</p>
          <h2>Transfer activity</h2>
        </div>
        <span className="panel-count">{transfers.length}</span>
      </div>

      {transfers.length === 0 ? (
        <EmptyState
          title="No transfers yet"
          detail="Submitted transfers will appear here and update as the worker processes them."
        />
      ) : (
        <div className="activity-list">
          {transfers.map((transfer) => (
            <article className="activity-row" key={transfer.id}>
              <div className="activity-route">
                <div className="route-node" aria-hidden="true" />
                <div>
                  <strong>
                    {accountNames.get(transfer.sourceAccountId) ??
                      shortId(transfer.sourceAccountId)}
                    <span aria-hidden="true"> → </span>
                    {accountNames.get(transfer.destinationAccountId) ??
                      shortId(transfer.destinationAccountId)}
                  </strong>
                  <span>
                    {formatDate(transfer.createdUtc)} · attempt {transfer.attemptCount}
                  </span>
                </div>
              </div>
              <strong className="activity-amount">
                {formatMoney(transfer.amount)}
              </strong>
              <StatusBadge status={transfer.status} />
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
