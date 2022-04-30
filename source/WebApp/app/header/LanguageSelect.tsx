import React, { FC } from 'react';
import { languages, LanguageName } from '../../ts/helpers/languages';
import { SelectHTMLProps, Select } from '../shared/Select';
import { useAndSetOption } from '../shared/useOption';

type Props = {
    useAriaLabel?: boolean;
} & Omit<SelectHTMLProps, 'aria-label'>;

const options = [
    { label: 'C#', value: languages.csharp },
    { label: 'Visual Basic', value: languages.vb },
    { label: 'F#', value: languages.fsharp },
    { label: 'IL', value: languages.il }
] as const;

export const LanguageSelect: FC<Props> = ({ useAriaLabel, ...htmlProps }) => {
    const [language, setLanguage] = useAndSetOption('language');

    return <Select<LanguageName>
        className="option-language option online-only"
        value={language}
        options={options}
        onSelect={setLanguage}
        // eslint-disable-next-line no-undefined
        aria-label={useAriaLabel ? 'Code Language' : undefined}
        {...htmlProps}
    />;
};