import React, { Context, ReactNode, useMemo } from 'react';
import type { MutableContextValue } from '../../shared/contexts/MutableContextValue';

type Props<TValue> = {
    context: Context<MutableContextValue<TValue>>;
    children: ReactNode;
    value: MutableContextValue<TValue>[0];
    setValue: MutableContextValue<TValue>[1];
};

export const MutableValueProvider = <TValue, >(
    { context, value, setValue, children }: Props<TValue>
) => {
    // eslint-disable-next-line react-hooks/exhaustive-deps
    const contextValue = useMemo(() => [value, setValue] as const, [value]);
    const Context = context;

    return <Context.Provider value={contextValue}>
        {children}
    </Context.Provider>;
};