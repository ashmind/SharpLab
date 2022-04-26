import React, { FC } from 'react';
import { ErrorsTopSection } from './ErrorsTopSection';
import { classNames } from './helpers/classNames';
import { ResultsTopSection, ResultsTopSectionProps } from './ResultsTopSection';
import { WarningsTopSection } from './WarningsTopSection';

type Props = ResultsTopSectionProps & {
    loading: boolean;
};

export const ResultsTopSectionGroup: FC<Props> = ({ loading, ...props }) => {
    const { result } = props;

    return <div className={classNames('top-section-group top-section-group-results', loading && 'loading')}>
        <ResultsTopSection {...props} />
        <ErrorsTopSection errors={result.errors} />
        <WarningsTopSection warnings={result.warnings} />
    </div>;
};