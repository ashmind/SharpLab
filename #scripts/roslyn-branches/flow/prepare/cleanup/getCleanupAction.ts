import { differenceInDays } from 'date-fns';
import type { CleanupAction, RoslynBranch } from '../../../shared/types';

const DAYS_UNTIL_STOP = 3;
const DAYS_UNTIL_DELETE = 7;

export const getCleanupAction = (branch: RoslynBranch, merged: boolean): CleanupAction => {
    if (!merged)
        return 'fail-not-merged';

    if (!branch.merged)
        return 'mark-as-merged';

    const mergeDetected = new Date(branch.mergeDetected);
    if (differenceInDays(new Date(), mergeDetected) < DAYS_UNTIL_STOP)
        return 'wait';

    const stopped = branch.sharplab?.stopped
        ? new Date(branch.sharplab.stopped)
        : null;
    if (!stopped)
        return 'stop';

    if (differenceInDays(new Date(), stopped) < DAYS_UNTIL_DELETE)
        return 'wait';

    return 'delete';
};