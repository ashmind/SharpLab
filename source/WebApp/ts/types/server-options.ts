import type { ServerOptions as MirrorSharpServerOptions } from 'mirrorsharp';
import type { TargetName } from '../helpers/targets';

export interface ServerOptions extends MirrorSharpServerOptions {
    'x-optimize': 'release'|'debug';
    'x-target': TargetName|string;
}