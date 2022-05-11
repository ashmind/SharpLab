import { Context, createContext } from 'react';
import type { TargetName } from '../../../ts/helpers/targets';
import type { Branch } from '../../../ts/types/branch';
import type { MutableContextValue } from './MutableContextValue';

export type OptionTypeMap = {
    target: TargetName;
    release: boolean;
    branch: Branch | null;
};
export type OptionName = keyof OptionTypeMap;

export type OptionContext<TOptionName extends OptionName> = MutableContextValue<OptionTypeMap[TOptionName]>;

export const optionContexts = {
    /* eslint-disable @typescript-eslint/no-non-null-assertion */
    target: createContext<OptionContext<'target'>>(null!),
    release: createContext<OptionContext<'release'>>(null!),
    branch: createContext<OptionContext<'branch'>>(null!)
    /* eslint-restore @typescript-eslint/no-non-null-assertion */
} as {
    [TName in OptionName]: Context<OptionContext<TName>>
};