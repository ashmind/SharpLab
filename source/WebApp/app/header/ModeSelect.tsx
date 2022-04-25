import React, { FC } from 'react';
import { Select, SelectHTMLProps } from 'app/shared/Select';

type Mode = 'debug'|'release';

type Props = {
    mode: 'debug'|'release';
    onSelect: (target: Mode) => void;
    useAriaLabel?: boolean;
    htmlProps?: Omit<SelectHTMLProps, 'aria-label'>;
};

const options = [
    { label: 'Debug', value: 'debug' },
    { label: 'Release', value: 'release' }
] as const;

export const ModeSelect: FC<Props> = ({ mode, onSelect, useAriaLabel }) => {
    return <Select<Mode>
        className="option-optimizations option online-only"
        value={mode}
        options={options}
        onSelect={onSelect}
        htmlProps={{
            // eslint-disable-next-line no-undefined
            'aria-label': useAriaLabel ? 'Build Mode' : undefined
        }}
    />;
};