import type { AstItem } from '../../../shared/resultTypes';

const EMPTY_SET = new Set<AstItem>() as ReadonlySet<AstItem>;

export type SelectionAction = {
    type: 'enable-hover-selection';
} |{
    type: 'select-from-child';
    item: AstItem;
} | {
    type: 'select-from-external-offset';
    selectedPath: ReadonlyArray<AstItem> | null;
} | {
    type: 'deselect-all';
};

export type SelectionState = {
    selectionMode: 'hover'|'click';
    // only applies if selection comes from outside of the component, e.g. selectedOffset
    expansionPath: ReadonlySet<AstItem>;
    expansionPersistent: boolean;
} & ({
    selectedItem: null;
    selectedItemSource?: null;
} | {
    selectedItem: AstItem;
    selectedItemSource: 'internal'|'external';
});

export const DEFAULT_SELECTION_STATE: SelectionState = {
    selectionMode: 'click',
    selectedItem: null,
    expansionPath: new Set(),
    expansionPersistent: false
};

export const selectionReducer = (state: SelectionState, action: SelectionAction): SelectionState => {
    switch (action.type) {
        case 'enable-hover-selection': {
            return {
                ...state,
                selectionMode: 'hover'
            };
        }

        case 'select-from-child': {
            const { item } = action;
            return {
                ...state,
                selectedItem: item,
                selectedItemSource: 'internal',
                expansionPath: EMPTY_SET
            };
        }

        case 'select-from-external-offset': {
            const { selectedPath } = action;
            const selectedItem = selectedPath?.[selectedPath.length - 1] ?? null;
            if (!selectedItem) {
                return {
                    ...state,
                    selectedItem: null,
                    selectedItemSource: null,
                    expansionPath: EMPTY_SET
                };
            }

            return {
                ...state,
                selectedItem,
                selectedItemSource: 'external',
                expansionPath: new Set(selectedPath)
            };
        }

        case 'deselect-all': {
            return {
                ...state,
                selectedItem: null,
                selectedItemSource: null,
                expansionPath: EMPTY_SET
            };
        }
    }
};