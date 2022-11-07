import { safeGetArgument } from '../../shared/safeGetArgument';

const branchName = safeGetArgument(0, 'Branch name');
const commit = safeGetArgument(1, 'Branch commit');
const app = safeGetArgument(2, 'App name');

