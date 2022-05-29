import { useId } from 'react';

type IdMap<T extends Array<string>> = { [TKey in T[number]]: string };
export const useIds = <T extends [string, string, ...Array<string>]>(names: [...T]): IdMap<T> => {
    const id = useId();

    const ids = {} as IdMap<T>;
    for (const name of names) {
        ids[name] = `${id}-${name}`;
    }
    return ids;
};