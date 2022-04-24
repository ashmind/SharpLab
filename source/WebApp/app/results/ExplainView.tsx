import React, { Fragment } from 'react';
import { marked } from 'marked';
import type { Explanation } from 'ts/types/results';

type Props = {
    explanations: ReadonlyArray<Explanation>;
};

const a = (string: string) => {
    if (/\s.*ing$/.test(string))
        return '';
    return /^[aeiou]/.test(string) ? 'an' : 'a';
};

const EMPTY = <section className="markdown">
    <p>
        🔬 This feature explains new and less-known syntax (e.g. <code>ref struct</code> or <code>@var</code>).<br />
        Your code either does not have any rare syntax, or the syntax is too new and not explained yet.
    </p>
    <p>
        If you would like something explained, please report at <a href="https://github.com/ashmind/language-syntax-explanations/issues">language-syntax-explanations/issues</a>.
    </p>
</section>;

export const ExplainView: React.FC<Props> = ({ explanations }) => {
    const getExplanationHtml = (text: string) => ({
        __html: marked(text)
    });

    const renderExplanation = ({ code, name, text, link }: Explanation, index: number) => <Fragment key={index}>
        <dt>
            <code>{code.trim()}</code> is {a(name)} <dfn>{name}</dfn>.
            <span className="explanation-doc-link">[<a href={link} target="_blank">Docs</a>]</span>
        </dt>
        <dd dangerouslySetInnerHTML={getExplanationHtml(text)} className="markdown" />
    </Fragment>;

    return <div className="result-content explanations">{
        explanations.length > 0 ? <dl>{explanations.map(renderExplanation)}</dl> : EMPTY
    }</div>;
};