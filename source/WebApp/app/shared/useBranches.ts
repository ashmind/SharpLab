import { useContext } from 'react';
import { BranchesContext } from './contexts/BranchesContext';

export const useBranches = () => useContext(BranchesContext);