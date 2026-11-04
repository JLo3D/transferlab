const workspaceStorageKey = "transferlab.workspace.v1";

export interface WorkspaceIndex {
  accountIds: string[];
  transferIds: string[];
}

const emptyWorkspace: WorkspaceIndex = {
  accountIds: [],
  transferIds: [],
};

export function loadWorkspace(): WorkspaceIndex {
  try {
    const stored = localStorage.getItem(workspaceStorageKey);
    if (!stored) return emptyWorkspace;

    const parsed = JSON.parse(stored) as Partial<WorkspaceIndex>;

    return {
      accountIds: Array.isArray(parsed.accountIds) ? parsed.accountIds : [],
      transferIds: Array.isArray(parsed.transferIds) ? parsed.transferIds : [],
    };
  } catch {
    return emptyWorkspace;
  }
}

export function saveWorkspace(workspace: WorkspaceIndex): void {
  localStorage.setItem(workspaceStorageKey, JSON.stringify(workspace));
}

export function registerAccount(
  workspace: WorkspaceIndex,
  accountId: string,
): WorkspaceIndex {
  if (workspace.accountIds.includes(accountId)) return workspace;

  return {
    ...workspace,
    accountIds: [...workspace.accountIds, accountId],
  };
}

export function registerTransfer(
  workspace: WorkspaceIndex,
  transferId: string,
): WorkspaceIndex {
  if (workspace.transferIds.includes(transferId)) return workspace;

  return {
    ...workspace,
    transferIds: [transferId, ...workspace.transferIds],
  };
}
