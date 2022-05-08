import React, { FC } from 'react';
import { CodeEditorSwitch } from './features/cm6-preview/CodeEditorPreviewSwitch';
import { MobileFontSizeSwitch } from './features/mobile-font-size/MobileFontSizeSwitch';
import { ThemeSwitch } from './footer/ThemeSwitch';

export const Footer: FC = () => {
    return <footer>
        <CodeEditorSwitch />
        <MobileFontSizeSwitch />
        <ThemeSwitch />
        <span className="footer-author-full">
            Built by <a href="http://twitter.com/ashmind">Andrey Shchekin (@ashmind)</a> — see <a href="http://github.com/ashmind/SharpLab">SharpLab on GitHub</a>.
        </span>
        <span className="footer-author-mobile">
            <a href="http://github.com/ashmind/SharpLab">SharpLab</a> by <a href="http://twitter.com/ashmind">Andrey Shchekin (@ashmind)</a>
        </span>
    </footer>;
};