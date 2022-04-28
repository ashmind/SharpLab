import { useContext } from 'react';
import { ResultContext } from './contexts/ResultContext';

export const useResult = () => useContext(ResultContext)[0];
export const useDispatchResultUpdate = () => useContext(ResultContext)[1];