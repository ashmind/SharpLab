import './app-code-edit';
import './app-code-view';
import './app-ast-view';
import './app-verify-view';
import './app-explain-view';
import './app-output-view';
import './app-gist-manager';
import './app-section-branch-details';
import './app-mobile-settings';
import './app-footer';
import { WarningSection } from 'app/results/diagnostics/WarningSection';
import { Diagnostic } from 'app/results/diagnostics/Diagnostic';

export const reactComponents = {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    'react-warnings-section': WarningSection as any,

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    'react-diagnostic': Diagnostic as any
} as const;