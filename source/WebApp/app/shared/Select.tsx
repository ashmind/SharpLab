import React, { ChangeEvent, useMemo } from 'react';

type Option<TValue> = {
    readonly value: TValue;
    readonly label: string;
};

type OptionGroup<TValue> = {
    readonly groupLabel: string;
    readonly options: ReadonlyArray<Option<TValue>>;
};

type Options<TValue> = ReadonlyArray<Option<TValue>|OptionGroup<TValue>>;
export type SelectHTMLProps = {
    id?: string;
    'aria-label'?: string;
    tabIndex?: number;
};

type Props<TValue extends string> = {
    value: TValue;
    options: Options<TValue>;
    onSelect: (value: TValue) => void;
    className?: string;
} & SelectHTMLProps;

const renderOptions = <TValue extends string>(options: Options<TValue>): ReadonlyArray<JSX.Element> => options.map((optionOrGroup, index) => {
    if ('groupLabel' in optionOrGroup) {
        const { groupLabel, options } = optionOrGroup;
        return <optgroup label={groupLabel} key={index}>
            {renderOptions(options)}
        </optgroup>;
    }

    const { value, label } = optionOrGroup;
    return <option value={value} key={index}>{label}</option>;
});

export const Select = <TValue extends string>({ value, options, onSelect, className, ...htmlProps }: Props<TValue>) => {
    const renderedOptions = useMemo(() => renderOptions(options), [options]);
    const onChange = (e: ChangeEvent<HTMLSelectElement>) => onSelect(e.target.value as TValue);

    return <div className={`select-wrapper${className ? (' ' + className) : ''}`}>
        <select value={value.toString()} onChange={onChange} {...htmlProps}>{renderedOptions}</select>
    </div>;
};