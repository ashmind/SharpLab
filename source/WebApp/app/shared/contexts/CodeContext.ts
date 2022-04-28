import { createContext } from 'react';
import type { MutableContextValue } from './MutableContextValue';

// eslint-disable-next-line @typescript-eslint/no-non-null-assertion
export const CodeContext = createContext<MutableContextValue<string>>(null!);