import { useContext } from 'react';
import { CodeContext } from './contexts/CodeContext';

export const useCode = () => useContext(CodeContext)[0];
export const useAndSetCode = () => useContext(CodeContext);