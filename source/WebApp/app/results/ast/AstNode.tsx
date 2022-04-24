import React, { useCallback, useContext, useEffect, useLayoutEffect, useMemo, useRef, useState } from 'react';
import type { AstItem } from 'ts/types/results';
import { AstNodeList } from './AstNodeList';
import { AstSelectionContext } from './AstSelectionContext';

type Props = {
    item: AstItem;
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
    const elementRef = useRef<HTMLLIElement>(null);
    const [expanded, setExpanded] = useState<boolean>(false);
    const { selectionState, dispatchSelectionAction } = useContext(AstSelectionContext);
    const { selectionMode, selectedItem } = selectionState;

    const selected = selectedItem === item;

    useLayoutEffect(() => {
        if (selected && elementRef.current)
            elementRef.current.scrollIntoView();
    }, [selected]);

    useEffect(() => {
        if (selectionState.expansionPath.has(item))
            setExpanded(true);
    }, [item, selectionState.expansionPath]);

    const children = useMemo(() => {
        if (item.properties) {
            const childrenFromProperties = Object
                .entries(item.properties)
                .map(([name, value]) => ({ type: 'property-only', property: name, value } as AstItem));
            if (!item.children)
                return childrenFromProperties;

            return childrenFromProperties.concat(item.children);
        }

        return item.children;
    }, [item]);

    const onClick = useCallback(() => {
        setExpanded(e => !e);
        // select-on-click is only enabled in mobile and such
        if (selectionMode === 'click')
            dispatchSelectionAction({ type: 'select-from-child', item });
    }, [item, selectionMode, dispatchSelectionAction]);

    const onMouseOver = useCallback(() => {
        if (selectionMode === 'hover')
            dispatchSelectionAction({ type: 'select-from-child', item });
    }, [item, selectionMode, dispatchSelectionAction]);

    const hasChildren = !!children?.length;
    const className = [
        expanded ? null : 'collapsed',
        selected ? 'selected' : null,
        hasChildren ? null : 'leaf'
    ].filter(c => c).join(' ');

    return <li className={className} ref={elementRef}>
        <span className={`ast-item-wrap ast-item-${item.type}`} onClick={onClick} onMouseOver={onMouseOver}>
            {hasChildren && <button />}
            <span className="ast-item-type" title={item.type}></span>
            {item.property && <span className="ast-item-property">{item.property}:</span>}
            {item.value && <span className="ast-inline-value">{renderValue(item.value, item.type)}</span>}
            <span className="ast-item-kind">{item.kind}</span>
        </span>
        {hasChildren && <AstNodeList items={children} />}
    </li>;
};