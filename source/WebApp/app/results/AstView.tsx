import React, { FC, useCallback, useEffect, useMemo, useReducer } from 'react';
import type { AstItem } from 'ts/types/results';
import { AstNodeList } from './ast/AstNodeList';
import { AstSelectionContext } from './ast/AstSelectionContext';
import { findItemPathByOffset } from './ast/findItemPathByOffset';
import { DEFAULT_SELECTION_STATE, selectionReducer } from './ast/selection';

type Props = {
    roots: ReadonlyArray<AstItem>;
    onSelect: (item: AstItem | null) => void;
    selectedOffset?: number;
};

export const AstView: FC<Props> = ({ roots, onSelect, selectedOffset }) => {
    const [selectionState, dispatchSelectionAction] = useReducer(selectionReducer, DEFAULT_SELECTION_STATE);

    useEffect(() => onSelect(selectionState.selectedItem), [selectionState.selectedItem, onSelect]);
    useEffect(() => {
        if (!selectedOffset)
            return;

        const selectedPath = findItemPathByOffset(roots, selectedOffset);
        dispatchSelectionAction({ type: 'select-from-external-offset', selectedPath });
    }, [roots, selectedOffset]);

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