import React, { FC } from 'react';
import { Select, SelectHTMLProps } from 'app/shared/Select';
import { LanguageName, languages } from 'ts/helpers/languages';

type Props = {
    language: LanguageName;
    onSelect: (language: LanguageName) => void;
    useAriaLabel?: boolean;
    htmlProps?: Omit<SelectHTMLProps, 'aria-label'>;
};

const options = [
    { label: 'C#', value: languages.csharp },
    { label: 'Visual Basic', value: languages.vb },
    { label: 'F#', value: languages.fsharp },
    { label: 'IL', value: languages.il }
] as const;

export const LanguageSelect: FC<Props> = ({ language, onSelect, useAriaLabel }) => {
    return <Select<LanguageName>
        className="option-language option online-only"
        value={language}
        options={options}
        onSelect={onSelect}
        htmlProps={{
            // eslint-disable-next-line no-undefined
            'aria-label': useAriaLabel ? 'Code Language' : undefined
        }}
    />;
};