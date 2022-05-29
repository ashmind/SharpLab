import { branchesPromise } from './internal/branchesPromise';

export async function resolveBranchAsync(branchId: string) {
    const branches = await branchesPromise;
    return branches.find(b => b.id === branchId) ?? null;
}