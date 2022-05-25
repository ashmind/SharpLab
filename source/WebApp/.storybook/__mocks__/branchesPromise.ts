import type { Branch } from '../../app/features/roslyn-branches/types';

const mockBranches = [] as Array<Branch>;

export const setMockBranches = (branches: ReadonlyArray<Branch>) => {
    mockBranches.splice(0, mockBranches.length);
    mockBranches.push(...branches);
};
export const branchesPromise = {
    then: (callback: (branches: ReadonlyArray<Branch>) => void) => callback([...mockBranches])
};