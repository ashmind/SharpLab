import React from 'react';
import ReactDOM from 'react-dom';
import { CodeEditorSwitch } from '../features/cm6-preview/CodeEditorSwitch';
import { DarkModeSwitch } from '../features/dark-mode/DarkModeSwitch';
import { MobileFontSizeSwitch } from '../features/mobile-font-size/MobileFontSizeSwitch';

// eslint-disable-next-line @typescript-eslint/no-non-null-assertion
const footerRoot = document.querySelector('body > footer')!;

export const Footer: React.FC = () => {
    const content = <>
        <CodeEditorSwitch />
        <MobileFontSizeSwitch />
        <DarkModeSwitch />
        <span className="footer-author-full">
            Built by <a href="https://linktr.ee/ashmind">Andrey Shchekin (@ashmind)</a> — see <a href="https://github.com/ashmind/SharpLab">SharpLab on GitHub</a>.
        </span>
        <span className="footer-author-mobile">
            <a href="https://github.com/ashmind/SharpLab">SharpLab</a> by <a href="https://linktr.ee/ashmind">Andrey Shchekin (@ashmind)</a>
        </span>
    </>;

    // Footer is using a portal since it should be under <footer>
    // rather than <main>, but React does not recommend attaching
    // <App> to <body>.
    return ReactDOM.createPortal(content, footerRoot);
};