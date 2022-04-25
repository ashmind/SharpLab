import { useExpander } from 'app/helpers/useExpander';
import React, { FC } from 'react';
import type { Branch } from 'ts/types/branch';

type Props = {
    className?: string;
    branch: Branch;
    headerless?: boolean;
};

export const BranchDetailsSection: FC<Props> = ({ branch, headerless }) => {
    const { applyExpanderToClassName, ExpanderButton } = useExpander();

    if (!branch.commits)
        return null;

    const className = applyExpanderToClassName('details-only branch-details non-code block-section');
    return <section className={className}>
        {!headerless && <header v-if="header">
            <ExpanderButton />
            <h1>Branch {branch.displayName}</h1>
        </header>}

        <div className="content">
            {branch.feature?.url && <div className="branch-feature-link">
                <a href={branch.feature.url} target="_blank">{branch.feature.url}</a>
            </div>}
            <div>
                Latest commit
                <a href={`https://github.com/dotnet/roslyn/commit/${branch.commits[0].hash}`} target="_blank">{branch.commits[0].hash.substring(0, 7)}</a>
                by {branch.commits[0].author}:
            </div>
            <div className="branch-commit-message">{branch.commits[0].message.trim()}</div>
        </div>
    </section>;
};