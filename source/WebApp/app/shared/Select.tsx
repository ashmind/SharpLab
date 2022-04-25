import React, { ChangeEvent } from 'react';

type Props<TValue> = {
    value: TValue;
    options: ReadonlyArray<{
        value: TValue;
        label: string;
    }>;
    onSelect: (value: TValue) => void;
};

export const Select = <TValue, >({ value, options, onSelect }: Props<TValue>) => {
    const valueIndex = options.findIndex(o => o.value === value);
    const onChange = (e: ChangeEvent<HTMLSelectElement>) => onSelect(options[parseInt(e.target.value, 10)].value);

    return <div className="select-wrapper">
        <select value={valueIndex} onChange={onChange}>
            {options.map(({ label }, index) => <option key={index} value={index}>{label}</option>)}
        </select>
    </div>;
};