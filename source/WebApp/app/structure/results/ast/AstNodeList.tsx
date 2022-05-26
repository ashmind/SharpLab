import React from 'react';
import type { AstItem } from '../../../shared/resultTypes';
import { AstNode } from './AstNode';

type Props = {
    items: ReadonlyArray<AstItem>;
};

export const AstNodeList: React.FC<Props> = ({ items }) => {
    return <ol>{items.map((c, index) => <AstNode key={index} item={c} />)}</ol>;
};