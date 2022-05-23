import { useMemo } from 'react';
import { useRecoilValue } from 'recoil';
import { branchOptionState } from '../../../features/roslyn-branches/branchOptionState';

export const useServiceUrl = () => {
    const branch = useRecoilValue(branchOptionState);
    return useMemo(() => {
        const httpRoot = branch ? branch.url : window.location.origin;
        return `${httpRoot.replace(/^http/, 'ws')}/mirrorsharp`;
    }, [branch]);
};