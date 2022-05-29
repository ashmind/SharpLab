import { useEffect } from 'react';
import type { Branch } from '../../app/features/roslyn-branches/types';

type ResolveBranches = (branches: ReadonlyArray<Branch>) => void;

let mockBranches = [] as ReadonlyArray<Branch>;
const resolves = [] as Array<ResolveBranches>;

export const useMockBranches = (branches: ReadonlyArray<Branch>) => useEffect(() => {
    const previous = mockBranches;
    mockBranches = branches;
    for (const resolve of resolves) {
        resolve(mockBranches!);
    }
    return () => { mockBranches = previous; };
}, []);

export const branchesPromise = {
    then: (resolve: ResolveBranches) => {
        resolve(mockBranches);
        resolves.push(resolve);
    }
};