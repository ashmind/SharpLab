import { useEffect, useState } from 'react';

export const useStoredState = <T extends string>(storageKey: string, defaultValue: T) => {
    const [value, setValue] = useState(defaultValue);

    useEffect(() => {
        const storedValue = localStorage[storageKey];
        if (storedValue)
            setValue(storedValue);
    }, [storageKey]);

    useEffect(() => {
        localStorage[storageKey] = value;
    }, [storageKey, value]);

    return [value, setValue] as const;
};