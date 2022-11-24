import { WebSiteManagementClient } from '@azure/arm-appservice';
import { AZURE_RESOURCE_GROUP_NAME } from '../../shared/azureResourceGroupName';
import { getBranchesJson, updateInBranchesJson } from '../../shared/branchesJson';
import { getAzureCredentialWithSubscriptionId } from '../../shared/getAzureCredential';
import { nodeSafeTopLevelAwait } from '../../shared/nodeSafeTopLevelAwait';
import { safeGetArgument } from '../../shared/safeGetArgument';
import type { CleanupAction } from '../../shared/types';
import { useAzure } from '../../shared/useAzure';

const branchName = safeGetArgument(0, 'Branch name');
const action = safeGetArgument<Exclude<CleanupAction, 'wait'>>(1, 'Action');

const run = async () => {
    if (!useAzure)
        throw 'Non-Azure cleanup is not supported';

    if (action === 'fail-not-merged')
        throw 'Unsupported state: branch deleted, but not merged.';

    console.log(`Action: ${action}`);

    console.log(`Finding branch ${branchName} in branches.json...`);
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const branch = (await getBranchesJson()).find(b => b.name === branchName)!;
    if (branch.kind !== 'roslyn')
        throw `Unexpected branch kind: ${branch.kind}.`;

    const isoNow = (new Date()).toISOString();
    if (action === 'mark-as-merged') {
        console.log('Updating branches.json...');
        console.log('  merged: true');
        console.log(`  mergeDetected: ${isoNow}`);
        await updateInBranchesJson({
            ...branch,
            merged: true,
            mergeDetected: isoNow
        });
        return;
    }

    if (!branch.merged)
        throw `Unexpected attempt to stop or delete non-merged branch.`;

    const { credential, subscriptionId } = await getAzureCredentialWithSubscriptionId();
    const azureWebAppClient = new WebSiteManagementClient(credential, subscriptionId);
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const webAppName = branch.url.match(/([^/.]+).azurewebsites.net/)![1];

    if (action === 'stop') {
        console.log(`Stopping web app ${webAppName}...`);
        await azureWebAppClient.webApps.stop(AZURE_RESOURCE_GROUP_NAME, webAppName);

        console.log('Updating branches.json...');
        console.log(`  sharplab stopped: ${isoNow}`);
        await updateInBranchesJson({
            ...branch,
            sharplab: {
                ...branch.sharplab ?? { supportsUnknownOptions: false },
                stopped: isoNow
            }
        });
        return;
    }

    // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
    if (action !== 'delete') {
        // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
        throw `Unsupported action: ${action}`;
    }

    if (!branch.sharplab?.stopped)
        throw 'Unexpected attempt to delete non-stopped branch.';

    console.log(`Deleting web app ${webAppName}...`);
    await azureWebAppClient.webApps.delete(AZURE_RESOURCE_GROUP_NAME, webAppName);
    console.log('Updating branches.json...');
    console.log(`  sharplab deleted: ${isoNow}`);
    await updateInBranchesJson({
        ...branch,
        sharplab: {
            ...branch.sharplab,
            deleted: isoNow
        }
    });

    console.log('Done.');
};

nodeSafeTopLevelAwait(run, e => {
    console.error('::error::' + e);
    process.exit(1);
}, { timeoutMinutes: 10 });