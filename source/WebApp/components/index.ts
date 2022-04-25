import './app-code-edit';
import './app-code-view';
import './app-ast-view';
import './app-gist-manager';
import './app-section-branch-details';
import './app-mobile-settings';
import { WarningSection } from 'app/results/diagnostics/WarningSection';
import { Diagnostic } from 'app/results/diagnostics/Diagnostic';
import { Footer } from 'app/Footer';
import { VerifyView } from 'app/results/VerifyView';
import { ExplainView } from 'app/results/ExplainView';
import { OutputView } from 'app/results/OutputView';

/* eslint-disable @typescript-eslint/no-explicit-any */
export const reactComponents = {
    'react-explain-view': ExplainView as any,
    'react-output-view': OutputView as any,
    'react-verify-view': VerifyView as any,
    'react-warnings-section': WarningSection as any,
    'react-diagnostic': Diagnostic as any,
    'react-footer': Footer as any
} as const;
/* eslint-restore @typescript-eslint/no-explicit-any */