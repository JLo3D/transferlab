import type { TransferStatus } from "../api/types";

interface StatusBadgeProps {
  status: TransferStatus;
}

export function StatusBadge({ status }: StatusBadgeProps) {
  return (
    <span className={`status-badge status-${status.toLowerCase()}`}>
      <span className="status-dot" aria-hidden="true" />
      {status}
    </span>
  );
}
