import React, { FC } from 'react';
import type { AstItem } from 'ts/types/results';
import { AstNode } from './AstNode';

type Props = {
    items: ReadonlyArray<AstItem>;
};

export const AstNodeList: FC<Props> = ({ items }) => {
    return <ol>{items.map((c, index) => <AstNode key={index} item={c} />)}</ol>;
};