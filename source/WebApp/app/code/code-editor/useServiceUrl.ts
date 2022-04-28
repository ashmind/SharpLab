import { useOption } from 'app/shared/useOption';
import { useMemo } from 'react';

export const useServiceUrl = () => {
    const branch = useOption('branch');
    return useMemo(() => {
        const httpRoot = branch ? branch.url : window.location.origin;
        return `${httpRoot.replace(/^http/, 'ws')}/mirrorsharp`;
    }, [branch]);
};