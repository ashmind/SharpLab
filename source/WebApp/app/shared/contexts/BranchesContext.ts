import { createContext } from 'react';
import type { Branch } from 'ts/types/branch';

export const BranchesContext = createContext<ReadonlyArray<Branch>>([]);