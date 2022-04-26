import React, { FC } from 'react';
import { CodeTopSection, CodeTopSectionProps } from './CodeTopSection';
import { BranchDetailsSection } from './code/BranchDetailsSection';

type Props = CodeTopSectionProps;

export const CodeTopSectionGroup: FC<Props> = props => {
    const { options: { branch } } = props;
    return <div className="top-section-group top-section-group-code">
        <CodeTopSection {...props} />
        {branch && <BranchDetailsSection branch={branch} className="top-section" />}
    </div>;
};