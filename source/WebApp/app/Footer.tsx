import React, { FC } from 'react';
import { EditorSwitch } from './footer/EditorSwitch';
import { MobileFontSizeSwitch } from './footer/MobileFontSizeSwitch';
import { ThemeSwitch } from './footer/ThemeSwitch';

export const Footer: FC = () => {
    return <footer>
        <EditorSwitch />
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