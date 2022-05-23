import { useRef, useState } from 'react';

export const useLoadingWait = () => {
    const [loading, setLoading] = useState(false);
    const delayTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    return {
        loading,
        onWait: () => {
            if (delayTimerRef.current)
                return;
            delayTimerRef.current = setTimeout(() => {
                setLoading(true);
                delayTimerRef.current = null;
            }, 300);
        },
        endWait: () => {
            setLoading(false);
            if (!delayTimerRef.current)
                return;
            clearTimeout(delayTimerRef.current);
            delayTimerRef.current = null;
        }
    };
};