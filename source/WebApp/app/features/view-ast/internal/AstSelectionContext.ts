import { createContext } from 'react';
import { DEFAULT_SELECTION_STATE, SelectionAction, SelectionState } from './selection';

type Context = {
    selectionState: SelectionState;
    dispatchSelectionAction: (action: SelectionAction) => void;
};

export const AstSelectionContext = createContext<Context>({
    selectionState: DEFAULT_SELECTION_STATE,
    // eslint-disable-next-line @typescript-eslint/no-empty-function
    dispatchSelectionAction: () => {}
});