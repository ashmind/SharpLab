import { useEffect, useMemo, useState } from 'react';
import { useRecoilValue } from 'recoil';
import type { ServerOptions } from '../../../ts/types/server-options';
import { branchOptionState } from '../../shared/state/branchOptionState';
import { targetOptionState } from '../../shared/state/targetOptionState';
import { useOption } from '../../shared/useOption';

export const useServerOptions = ({ initialCached }: { initialCached: boolean }): ServerOptions => {
    const branch = useRecoilValue(branchOptionState);
    const release = useOption('release');
    const target = useRecoilValue(targetOptionState);
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