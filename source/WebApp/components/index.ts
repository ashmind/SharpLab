import './app-code-edit';
import './app-code-view';
import './app-ast-view';
import './app-verify-view';
import './app-explain-view';
import './app-output-view';
import './app-gist-manager';
import './app-section-branch-details';
import './app-mobile-settings';
import { WarningSection } from 'app/results/diagnostics/WarningSection';
import { Diagnostic } from 'app/results/diagnostics/Diagnostic';
import { Footer } from 'app/Footer';

/* eslint-disable @typescript-eslint/no-explicit-any */
export const reactComponents = {
    'react-warnings-section': WarningSection as any,
    'react-diagnostic': Diagnostic as any,
    'react-footer': Footer as any
} as const;
/* eslint-restore @typescript-eslint/no-explicit-any */