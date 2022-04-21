import React from 'react';
import type { AstItem } from 'ts/types/results';

type Props = {
    item: AstItem
};

const escapeCommon = (value: string) => {
    return value
        .replace('\r', '\\r')
        .replace('\n', '\\n')
        .replace('\t', '\\t');
};

const escapeTrivia = (value: string) => {
    return escapeCommon(value)
        .replace(/(^ +| +$)/g, (_, $1: string) => $1.length > 1 ? `<space:${$1.length}>` : '<space>');
};

const renderValue = (value: string, type: string) =>{
    if (type === 'trivia')
        return escapeTrivia(value);

    return escapeCommon(value);
};

export const AstNode: React.FC<Props> = ({ item }) => {
    return <span className={`ast-item-wrap ast-item-${item.type}`}>
        {item.children && <button v-if="item.children"></button>}
        <span className="ast-item-type" title={item.type}></span>
        {item.property && <span className="ast-item-property" v-if="item.property">{item.property}:</span>}
        {item.value && <span className="ast-inline-value">{renderValue(item.value, item.type)}</span>}
        <span className="ast-item-kind">{item.kind}</span>
    </span>;
};