import { useCodeRangeSync } from 'app/shared/useCodeRangeSync';
import React, { FC, useCallback, useEffect, useMemo, useReducer } from 'react';
import type { AstItem } from 'ts/types/results';
import { AstNodeList } from './ast/AstNodeList';
import { AstSelectionContext } from './ast/AstSelectionContext';
import { findItemPathByOffset } from './ast/findItemPathByOffset';
import { parseRangeFromItem } from './ast/parseRangeFromItem';
import { DEFAULT_SELECTION_STATE, selectionReducer } from './ast/selection';

type Props = {
    roots: ReadonlyArray<AstItem>;
};

export const AstView: FC<Props> = ({ roots }) => {
    const [, setSourceRange] = useCodeRangeSync('source');
    const [targetOffset] = useCodeRangeSync('target');

    const [selectionState, dispatchSelectionAction] = useReducer(selectionReducer, DEFAULT_SELECTION_STATE);

    useEffect(() => {
        const range = parseRangeFromItem(selectionState.selectedItem);
        setSourceRange(range);
    }, [selectionState.selectedItem, setSourceRange]);
    useEffect(() => {
        if (!targetOffset)
            return;

        const selectedPath = findItemPathByOffset(roots, targetOffset);
        dispatchSelectionAction({ type: 'select-from-external-offset', selectedPath });
    }, [roots, targetOffset]);

    const onMouseOver = useMemo(() => {
        if (selectionState.selectionMode === 'hover')
            return;

        return () => dispatchSelectionAction({ type: 'enable-hover-selection' });
    }, [selectionState.selectionMode]);
    const onMouseOut = useCallback(() => dispatchSelectionAction({ type: 'deselect-all' }), []);

    const selectionContext = useMemo(() => ({
        selectionState,
        dispatchSelectionAction
    }), [selectionState, dispatchSelectionAction]);

    return <div className="ast" onMouseOver={onMouseOver} onMouseOut={onMouseOut}>
        <AstSelectionContext.Provider value={selectionContext}>
            <AstNodeList items={roots} />
        </AstSelectionContext.Provider>
    </div>;
};