import { useMemo } from 'react';
import { useOption } from '../../shared/useOption';

export const useServiceUrl = () => {
    const branch = useOption('branch');
    return useMemo(() => {
        const httpRoot = branch ? branch.url : window.location.origin;
        return `${httpRoot.replace(/^http/, 'ws')}/mirrorsharp`;
    }, [branch]);
};