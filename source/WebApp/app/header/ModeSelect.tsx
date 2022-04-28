import React, { FC } from 'react';
import { Select, SelectHTMLProps } from 'app/shared/Select';
import { useAndSetOption } from 'app/shared/useOption';

type Mode = 'debug'|'release';

type Props = {
    useAriaLabel?: boolean;
} & Omit<SelectHTMLProps, 'aria-label'>;

const options = [
    { label: 'Debug', value: 'debug' },
    { label: 'Release', value: 'release' }
] as const;

export const ModeSelect: FC<Props> = ({ useAriaLabel, ...htmlProps }) => {
    const [release, setRelease] = useAndSetOption('release');

    return <Select<Mode>
        className="option-optimizations option online-only"
        value={release ? 'release' : 'debug'}
        options={options}
        onSelect={v => setRelease(v === 'release')}
        // eslint-disable-next-line no-undefined
        aria-label={useAriaLabel ? 'Build Mode' : undefined}
        {...htmlProps}
    />;
};