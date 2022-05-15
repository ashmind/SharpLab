import { useEffect, useMemo, useState } from 'react';
import { useRecoilValue } from 'recoil';
import { branchOptionState } from '../../features/roslyn-branches/branchOptionState';
import { releaseOptionState } from '../../shared/state/releaseOptionState';
import { targetOptionState } from '../../shared/state/targetOptionState';
import type { ServerOptions } from './ServerOptions';

export const useServerOptions = ({ initialCached }: { initialCached: boolean }): ServerOptions => {
    const branch = useRecoilValue(branchOptionState);
    const target = useRecoilValue(targetOptionState);
    const release = useRecoilValue(releaseOptionState);
    const [wasCached, setWasCached] = useState(initialCached);

    useEffect(() => {
        // can only happen to first result
        if (initialCached)
            setWasCached(true);
    }, [initialCached]);

    const noCache = wasCached
        && (!branch || branch.sharplab?.supportsUnknownOptions);

    return useMemo(() => ({
        'x-optimize': release ? 'release' : 'debug',
        'x-target': target,
        ...(noCache ? { 'x-no-cache': true } : {})
    }), [release, target, noCache]);
};