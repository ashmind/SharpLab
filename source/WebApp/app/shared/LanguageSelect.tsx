import React, { FC } from 'react';
import { useRecoilState } from 'recoil';
import { LanguageName, LANGUAGE_CSHARP, LANGUAGE_FSHARP, LANGUAGE_IL, LANGUAGE_VB } from '../shared/languages';
import { SelectHTMLProps, Select } from '../shared/Select';
import { languageOptionState } from '../shared/state/languageOptionState';

type Props = {
    useAriaLabel?: boolean;
} & Omit<SelectHTMLProps, 'aria-label'>;

const options = [
    { label: 'C#', value: LANGUAGE_CSHARP },
    { label: 'Visual Basic', value: LANGUAGE_VB },
    { label: 'F#', value: LANGUAGE_FSHARP },
    { label: 'IL', value: LANGUAGE_IL }
] as const;

export const LanguageSelect: FC<Props> = ({ useAriaLabel, ...htmlProps }) => {
    const [language, setLanguage] = useRecoilState(languageOptionState);

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