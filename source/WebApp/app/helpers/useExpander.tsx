import React, { useMemo, useState } from 'react';

type Props = {
    setExpanded: (set: (expanded: boolean) => boolean) => void;
};

const ExpanderButton: React.FC<Props> = ({ setExpanded }) => {
    return <button className="expander" onClick={() => setExpanded(e => !e)}></button>;
};

export const useExpander = ({ initialExpanded = false }: { initialExpanded?: boolean } = {}) => {
    const [expanded, setExpanded] = useState(initialExpanded);
    const BoundExpanderButton = useMemo(() => () => <ExpanderButton setExpanded={setExpanded} />, []);

    return {
        expandedClassName: expanded ? null : 'collapsed',
        ExpanderButton: BoundExpanderButton
    };
};