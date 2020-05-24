import type { AppOptions } from '../types/app';
import type { RawOptions } from '../types/raw-options';

export default ({ language, release, target, branch }: AppOptions): RawOptions =>
    ({ language, release, target, branchId: branch?.id ?? null });