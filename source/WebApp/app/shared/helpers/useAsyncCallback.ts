import { useCallback, useState } from 'react';

export const useAsyncCallback = <T>(call: () => Promise<T>, deps: ReadonlyArray<unknown>) => {
    const [pending, setPending] = useState(false);
    const [result, setResult] = useState<T>();
    const [error, setError] = useState<unknown>();

    const start = useCallback(() => {
        const startAsync = async () => {
            setPending(true);
            try {
                setResult(await call());
            }
            catch (e) {
                setError(e);
            }
            finally {
                setPending(false);
            }
        };
        void(startAsync());
    // eslint-disable-next-line react-hooks/exhaustive-deps
    }, deps);

    return [start, result, error, pending] as const;
};