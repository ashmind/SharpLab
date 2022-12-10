import React from 'react';
import { useRecoilValue } from 'recoil';
import { codeState } from '../../../../shared/state/codeState';
import { languageOptionState } from '../../../../shared/state/languageOptionState';

const useGetExceptionNoticeContent = (exception: string) => {
    const language = useRecoilValue(languageOptionState);
    const code = useRecoilValue(codeState);

    if (language !== 'IL' && exception.includes('System.BadImageFormatException')) {
        return <>
            <p>Note: This exception is likely caused by SharpLab itself, and not the C# compiler.</p>

            <p>Try adding <code>[assembly: SharpLab.Runtime.NoILRewriting]</code> to your code.<br />
            If exception disappears, this is definitely a SharpLab issue, and should be <a href="https://github.com/ashmind/SharpLab/issues">reported as such</a>.</p>
        </>;
    }

    if (language === 'IL' && exception.includes("assembly '<Unknown>'. Index not found. (0x80131124)") && !code.includes('.assembly'))
        return <>Note: This error is often caused by a missing <code>.assembly X {'{}'}</code> block in IL code.</>;

    return null;
};

type Props = {
    exception: string;
};

export const ExceptionNotice: React.FC<Props> = ({ exception }) => {
    const content = useGetExceptionNoticeContent(exception);
    return content && <div className="inspection-exception-notice markdown">{content}</div>;
};