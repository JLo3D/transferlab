import type { Account } from "../api/types";
import { formatMoney, shortId } from "../lib/format";

interface AccountCardProps {
  account: Account;
  index: number;
}

export function AccountCard({ account, index }: AccountCardProps) {
  const initials = account.name
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0])
    .join("")
    .toUpperCase();

  return (
    <article className="account-card">
      <div className={`account-avatar avatar-${(index % 4) + 1}`}>
        {initials || "A"}
      </div>
      <div className="account-copy">
        <span className="account-name">{account.name}</span>
        <span className="account-id" title={account.id}>
          {shortId(account.id)}
        </span>
      </div>
      <strong className="account-balance">{formatMoney(account.balance)}</strong>
    </article>
  );
}
