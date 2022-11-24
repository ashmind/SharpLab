import { branchesPromise } from './internal/branchesPromise';

export const resolveBranchAsync = async (branchId: string) => {
    const branches = await branchesPromise;
    let branch = branches.find(b => b.id === branchId);
    if (branch?.merged)
        branch = branches.find(b => b.id === 'main');
    return branch ?? null;
};