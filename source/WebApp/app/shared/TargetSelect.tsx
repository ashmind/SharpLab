import React, { useMemo } from 'react';
import { useRecoilState } from 'recoil';
import { SelectHTMLProps, Select } from '../shared/Select';
import { targetOptionState } from '../shared/state/targetOptionState';
import { TargetName, TARGET_ASM, TARGET_AST, TARGET_CSHARP, TARGET_EXPLAIN, TARGET_IL, TARGET_RUN, TARGET_VB, TARGET_VERIFY } from '../shared/targets';

type Props = {
    useAriaLabel?: boolean;
} & Omit<SelectHTMLProps, 'aria-label'>;

export const TargetSelect: React.FC<Props> = ({ useAriaLabel, ...htmlProps }) => {
    const [target, setTarget] = useRecoilState(targetOptionState);

    const options = useMemo(() => [
        {
            groupLabel: 'Decompile',
            options: [
                { label: 'C#', value: TARGET_CSHARP },
                ...(target === TARGET_VB ? [{ label: 'Visual Basic', value: TARGET_VB } as const] : []),
                { label: 'IL', value: TARGET_IL },
                { label: 'JIT Asm', value: TARGET_ASM }
            ]
        },
        {
            groupLabel: 'Other',
            options: [
                { label: 'Syntax Tree', value: TARGET_AST },
                { label: 'Verify Only', value: TARGET_VERIFY },
                { label: 'Explain', value: TARGET_EXPLAIN }
            ]
        },
        {
            groupLabel: 'Experimental',
            options: [
                { label: 'Run', value: TARGET_RUN }
            ]
        }
    ] as const, [target]);

    return <Select<TargetName>
        className="option-target option online-only"
        value={target}
        options={options}
        onSelect={setTarget}
        // eslint-disable-next-line no-undefined
        aria-label={useAriaLabel ? 'Output Mode' : undefined}
        {...htmlProps}
    />;
};