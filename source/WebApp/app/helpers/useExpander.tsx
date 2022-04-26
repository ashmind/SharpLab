import React, { FC, useMemo, useState } from 'react';

type Props = {
    setExpanded: (set: (expanded: boolean) => boolean) => void;
};

const ExpanderButton: FC<Props> = ({ setExpanded }) => {
    return <button className="expander" onClick={() => setExpanded(e => !e)}></button>;
};

export const useExpander = () => {
    const [expanded, setExpanded] = useState(false);
    const BoundExpanderButton = useMemo(() => () => <ExpanderButton setExpanded={setExpanded} />, []);

    return {
        expandedClassName: expanded ? null : 'collapsed',
        ExpanderButton: BoundExpanderButton
    };
};