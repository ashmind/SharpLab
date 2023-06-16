import React from 'react';
import { useRecoilValue } from 'recoil';
import { classNames } from '../../shared/helpers/classNames';
import { useExpander } from '../../shared/helpers/useExpander';
import { branchOptionState } from './branchOptionState';

type Props = {
    className?: string;
    headerless?: boolean;
    // Storybook/Tests only
    initialState?: {
        expanded?: boolean;
    };
};

export const BranchDetailsSection: React.FC<Props> = ({ className, headerless, initialState }) => {
    const branch = useRecoilValue(branchOptionState);
    const { expandedClassName, ExpanderButton } = useExpander({
        // eslint-disable-next-line @typescript-eslint/prefer-nullish-coalescing
        initialExpanded: headerless || initialState?.expanded
    });

    if (!branch?.commits)
        return null;

    const fullClassName = classNames(
        className,
        'details-only branch-details non-code block-section',
        expandedClassName
    );
    const commit = branch.commits[0];
    return <section className={fullClassName}>
        {!headerless && <header>
            <ExpanderButton />
            <h1>Branch {branch.displayName}</h1>
        </header>}

        <div className="content">
            {branch.feature?.url && <div className="branch-feature-link">
                <a href={branch.feature.url} target="_blank">{branch.feature.url}</a>
            </div>}
            {commit && <>
                <div>
                    Latest commit{' '}
                    <a href={`https://github.com/dotnet/roslyn/commit/${commit.hash}`} target="_blank">{commit.hash.substring(0, 7)}</a>
                    {' '}by {commit.author}:
                </div>
                <div className="branch-commit-message">{commit.message.trim()}</div>
            </>}
        </div>
    </section>;
};