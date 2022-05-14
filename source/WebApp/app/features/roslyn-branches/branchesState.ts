import { atom, selector } from 'recoil';
import { branchesPromise } from './internal/branchesPromise';
import type { Branch } from './types';

// Selector can handle async as well, but this allows
// [] while loading (instead of a Suspense).
const branchesLoaderState = atom({
    key: 'branches-loader',
    default: [] as ReadonlyArray<Branch>,
    effects: [
        ({ setSelf }) => {
            void (branchesPromise.then(b => setSelf(b)));
        }
    ]
});

export const branchesState = selector({
    key: 'branches',
    get: ({ get }) => get(branchesLoaderState)
});