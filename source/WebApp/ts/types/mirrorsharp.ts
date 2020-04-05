import type { MirrorSharpOptions, DiagnosticData } from 'mirrorsharp';

// TODO: export from MirrorSharp
export type MirrorSharpSlowUpdateResult<TExtension = never> = Parameters<NonNullable<NonNullable<MirrorSharpOptions<TExtension>['on']>['slowUpdateResult']>>[0];
export type MirrorSharpConnectionState = 'open'|'error'|'close';

// TODO: fix in MirrorSharp
export type MirrorSharpDiagnostic = DiagnosticData & { id: string };