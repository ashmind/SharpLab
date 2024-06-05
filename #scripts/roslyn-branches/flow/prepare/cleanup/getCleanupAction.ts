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

    const { sharplab } = branch;

    if (!sharplab?.stopped)
        return 'stop';

    if (sharplab.deleted)
        return 'done';

    if (differenceInDays(new Date(), new Date(sharplab.stopped)) < DAYS_UNTIL_DELETE)
        return 'wait';

    return 'delete';
};