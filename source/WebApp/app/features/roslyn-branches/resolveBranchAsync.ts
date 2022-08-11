import { branchesPromise } from './internal/branchesPromise';

export const resolveBranchAsync = async (branchId: string) => {
    const branches = await branchesPromise;
    return branches.find(b => b.id === branchId) ?? null;
};