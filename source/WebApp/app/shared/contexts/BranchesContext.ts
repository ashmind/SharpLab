import { createContext } from 'react';
import type { Branch } from '../types/Branch';

export const BranchesContext = createContext<ReadonlyArray<Branch>>([]);