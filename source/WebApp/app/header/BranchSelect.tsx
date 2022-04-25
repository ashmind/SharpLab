import React, { FC, useMemo } from 'react';
import { Select, SelectHTMLProps } from 'app/shared/Select';
import type { Branch } from 'ts/types/branch';
import type { LanguageName } from 'ts/helpers/languages';
import { groupAndSortBranches } from './branches/groupAndSortBranches';

type Props = {
    allBranches: ReadonlyArray<Branch>;
    language: LanguageName;
    branch: Branch | null;
    onSelect: (branch: Branch | null) => void;
    useAriaLabel?: boolean;
    htmlProps?: Omit<SelectHTMLProps, 'aria-label'>;
};

export const BranchSelect: FC<Props> = ({ allBranches, language, branch, onSelect, useAriaLabel }) => {
    const options = useMemo(() => {
        // eslint-disable-next-line prefer-const
        let { ungrouped, groups } = groupAndSortBranches(allBranches);
        if (language === 'F#')
            groups = groups.filter(g => g.kind !== 'roslyn');

        return [
            { label: 'Default', value: '' },
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            ...ungrouped.map(({ displayName, id }) => ({ label: displayName!, value: id })),
            ...groups.map(({ name, branches }) => ({
                groupLabel: name,
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                options: branches.map(({ displayName, id }) => ({ label: displayName!, value: id }))
            }))
        ];
    }, [allBranches, language]);

    return <Select<string>
        className="option-language option online-only"
        value={branch?.id ?? ''}
        options={options}
        onSelect={id => onSelect(allBranches.find(b => b.id === id) ?? null)}
        htmlProps={{
            // eslint-disable-next-line no-undefined
            'aria-label': useAriaLabel ? 'Platform or Roslyn branch' : undefined
        }}
    />;
};