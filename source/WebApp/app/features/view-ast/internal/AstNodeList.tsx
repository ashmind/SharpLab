import React from 'react';
import type { AstItem } from '../../../shared/resultTypes';
import { AstNode } from './AstNode';

type Props = {
    items: ReadonlyArray<AstItem>;
    // Storybook/Tests only
    initialState?: {
        expanded?: boolean;
    };
};

export const AstNodeList: React.FC<Props> = ({ items, initialState }) => {
    return <ol>{items.map((c, index) => <AstNode key={index} item={c} initialState={initialState} />)}</ol>;
};