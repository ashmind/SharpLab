import type { AstItem } from 'ts/types/results';

export type AstItemWithParent = AstItem & { parent?: AstItemWithParent };