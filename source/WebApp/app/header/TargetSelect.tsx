import React, { FC, useMemo } from 'react';
import { Select, SelectHTMLProps } from 'app/shared/Select';
import { TargetName, targets } from 'ts/helpers/targets';

type Props = {
    target: TargetName;
    onSelect: (target: TargetName) => void;
    useAriaLabel?: boolean;
    htmlProps?: Omit<SelectHTMLProps, 'aria-label'>;
};

export const TargetSelect: FC<Props> = ({ target, onSelect, useAriaLabel }) => {
    const options = useMemo(() => [
        {
            groupLabel: 'Decompile',
            options: [
                { label: 'C#', value: targets.csharp },
                ...(target === targets.vb ? [{ label: 'Visual Basic', value: targets.vb }] : []),
                { label: 'IL', value: targets.il },
                { label: 'JIT Asm', value: targets.asm }
            ]
        },
        {
            groupLabel: 'Other',
            options: [
                { label: 'Syntax Tree', value: targets.ast },
                { label: 'Verify Only', value: targets.verify },
                { label: 'Explain', value: targets.explain }
            ]
        },
        {
            groupLabel: 'Experimental',
            options: [
                { label: 'Run', value: targets.run }
            ]
        }
    ], [target]);

    return <Select<TargetName>
        className="option-target option online-only"
        value={target}
        options={options}
        onSelect={onSelect}
        htmlProps={{
            // eslint-disable-next-line no-undefined
            'aria-label': useAriaLabel ? 'Output Mode' : undefined
        }}
    />;
};