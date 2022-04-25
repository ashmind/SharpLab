import React, { FC, useCallback, useMemo, useState } from 'react';

type Props = {
    setExpanded: (set: (expanded: boolean) => boolean) => void;
};

const ExpanderButton: FC<Props> = ({ setExpanded }) => {
    return <button className="expander" onClick={() => setExpanded(e => !e)}></button>;
};

export const useExpander = () => {
    const [expanded, setExpanded] = useState(false);
    const applyClassName = useCallback((className: string) => className + (expanded ? '' : ' collapsed'), [expanded]);
    const BoundExpanderButton = useMemo(() => () => <ExpanderButton setExpanded={setExpanded} />, []);

    return {
        applyExpanderToClassName: applyClassName,
        ExpanderButton: BoundExpanderButton
    };
};