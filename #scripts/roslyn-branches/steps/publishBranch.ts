import path from 'path';
import fs from 'fs-extra';
import stripJsonComments from 'strip-json-comments';
import delay from 'delay';
import { ResourceManagementClient } from '@azure/arm-resources';
import { WebSiteManagementClient } from '@azure/arm-appservice';
import AdmZip from 'adm-zip';
import dateFormat from 'dateformat';
import safeFetch, { Response, SafeFetchError } from '../helpers/safeFetch';
import useAzure from '../helpers/useAzure';
import { getAzureCredentials } from './getAzureCredentials';

const resourceGroupName = 'SharpLab';

export default async function publishBranch({ webAppName, webAppUrl, branchArtifactsRoot, branchSiteRoot }: {
    webAppName: string;
    iisSiteName: string;
    webAppUrl: string;
    branchArtifactsRoot: string;
    branchSiteRoot: string;
}) {
    async function testBranchWebApp() {
        console.log(`GET ${webAppUrl}/status`);
        let ok = false;
        let tryPermanent = 1;
        let tryTemporary = 1;

        const formatStatus = ({ status, statusText }: Pick<Response, 'status'|'statusText'>) =>
            `  ${status} ${statusText}`;

        while (tryPermanent < 3 && tryTemporary < 30) {
            try {
                const response = await safeFetch(`${webAppUrl}/status`);
                ok = true;
                console.log(formatStatus(response));
                break;
            }
            catch (e) {
                // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
                if ((e as { response?: Response }).response) {
                    // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
                    console.warn(formatStatus((e as { response: Response }).response));
                }

                const { status } = (e as Partial<SafeFetchError>).response ?? {};
                const temporary = status === 503 || status === 403;
                if (temporary) {
                    tryTemporary += 1;
                }
                else {
                    tryPermanent += 1;
                }
                console.warn(e);
            }
            await delay(1000);
        }

        if (!ok)
            throw new Error(`Failed to get success from ${webAppUrl}/status`);
    }

    async function publishToAzure() {
        const armTemplate = JSON.parse(stripJsonComments(
            await fs.readFile(path.join(__dirname, '../arm/template.json'), 'utf-8')
        ));
        const armParameters = (JSON.parse(await fs.readFile(path.join(__dirname, '../arm/parameters.json'), 'utf-8')) as {
            parameters: Record<string, { value: string }>;
        }).parameters;

        console.log(`Deploying to Azure, ${webAppName}...`);

        const { credentials, subscriptionId } = await getAzureCredentials();

        const azureResourceClient = new ResourceManagementClient(credentials, subscriptionId);
        const azureWebAppClient = new WebSiteManagementClient(credentials, subscriptionId);

        console.log(`  Deploying web app...`);
        const result = await azureResourceClient.deployments.createOrUpdate(
            resourceGroupName,
            webAppName.replace(/^sl-b-/, 'sharplab-branch-'),
            {
                properties: {
                    mode: 'Incremental',
                    template: armTemplate,
                    parameters: {
                        // eslint-disable-next-line @typescript-eslint/camelcase
                        sites_name: { value: webAppName },
                        ...armParameters
                    }
                }
            }
        );
        console.log(`    Response:     ${result._response.status}`);
        console.log(`    Provisioning: ${result.properties?.provisioningState ?? '<null>'}`);

        console.log(`  Zipping...`);
        const zipPath = path.join(branchArtifactsRoot, 'Site.zip');
        console.log(`    => ${zipPath}`);
        const zip = new AdmZip();
        zip.addLocalFolder(branchSiteRoot);
        zip.writeZip(zipPath);

        console.log(`  Stopping...`);
        await azureWebAppClient.webApps.stop(resourceGroupName, webAppName);

        console.log(`  Publishing...`);
        const {
            publishingUserName,
            publishingPassword
        } = await azureWebAppClient.webApps.listPublishingCredentials(resourceGroupName, webAppName);
        const authHeader = {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            'Authorization': `Basic ${Buffer.from(`${publishingUserName}:${publishingPassword!}`).toString('base64')}`
        } as const;

        console.log(`    ⏱️ ${dateFormat(new Date(), 'HH:MM:ss')}`);
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const deploymentUrl = (await safeFetch(`https://${webAppName}.scm.azurewebsites.net/api/zipdeploy?isAsync=true`, {
            method: 'POST',
            body: fs.createReadStream(zipPath),
            headers: {
                ...authHeader,
                'Content-Length': (await fs.stat(zipPath)).size.toString()
            },
            redirect: 'manual'
        })).headers.get('Location')!;

        let deployment: {
            id: string;
            complete: boolean;
            provisioningState: 'Succeeded'|'Failed';
            log_url: string;
        };
        process.stdout.write('    ');
        try {
            do {
                process.stdout.write('░');
                await delay(500);
                deployment = await (await safeFetch(deploymentUrl, {
                    headers: {
                        ...authHeader
                    }
                })).json() as typeof deployment;
            } while (!deployment.complete);

            // https://github.com/projectkudu/kudu/issues/2906
            const logUrl = deployment.log_url.replace('/latest/', `/${deployment.id}/`);
            if (deployment.provisioningState !== 'Succeeded')
                throw new Error(`Deployment state: ${deployment.provisioningState}, logs at ${logUrl}`);
        }
        catch (e) {
            console.log('');
            throw e;
        }

        console.log('');
        console.log(`    ✔️ ${dateFormat(new Date(), 'HH:MM:ss')}`);

        console.log(`  Starting...`);
        await azureWebAppClient.webApps.start(resourceGroupName, webAppName);

        console.log(`  Done.`);
    }

    if (useAzure) {
        await publishToAzure();
    }
    else {
        // TODO: migrate to TypeScript
        throw new Error('Not migrated to TypeScript yet.');
    }

    await testBranchWebApp();
}