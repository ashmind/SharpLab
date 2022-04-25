import { useMemo } from 'react';
import { uid } from 'ts/ui/helpers/uid';

type IdMap<T extends Array<string>> = { [TKey in T[number]]: string };
export const useIds = <T extends Array<string>>(names: [...T]): IdMap<T> => {
    const id = useMemo(() => uid(), []);
    const result = {} as IdMap<T>;
    for (const name of names) {
        result[name] = 'uid-' + id + '-' + name;
    }
    return result;
};