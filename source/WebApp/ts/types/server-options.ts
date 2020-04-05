import type { ServerOptions as MirrorSharpServerOptions } from 'mirrorsharp';
import type { TargetName } from '../helpers/targets';

export interface ServerOptions extends MirrorSharpServerOptions {
    readonly 'x-optimize': 'release'|'debug';
    readonly 'x-target': TargetName|string;
}