import path from 'path';
import fs from 'fs-extra';
import stripJsonComments from 'strip-json-comments';
import delay from 'delay';
import { ResourceManagementClient } from '@azure/arm-resources';
import { WebSiteManagementClient } from '@azure/arm-appservice';
import AdmZip from 'adm-zip';
import dateFormat from 'dateformat';
import safeFetch from '../../helpers/safeFetch';
import { getAzureCredentials } from '../getAzureCredentials';

const resourceGroupName = 'SharpLab';
const armTemplatesBasePath = path.join(__dirname, '../../arm/');

const deployZip = async ({ webAppName, zipPath, userName, password }: {
    webAppName: string;
    zipPath: string;
    userName: string;
    password: string;
}) => {
    const authHeader = {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        'Authorization': `Basic ${Buffer.from(`${userName}:${password}`).toString('base64')}`
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
        console.log(`    ❌ ${dateFormat(new Date(), 'HH:MM:ss')}`);
        throw e;
    }

    console.log('');
    console.log(`    ✔️ ${dateFormat(new Date(), 'HH:MM:ss')}`);
};

export const publishToAzure = async ({
    webAppName,
    branchArtifactsRoot,
    branchSiteRoot
}: {
    webAppName: string;
    branchArtifactsRoot: string;
    branchSiteRoot: string;
}) => {
    const armTemplate = JSON.parse(stripJsonComments(
        await fs.readFile(path.join(armTemplatesBasePath, 'template.json'), 'utf-8')
    ));
    const armParameters = (JSON.parse(await fs.readFile(path.join(armTemplatesBasePath, 'parameters.json'), 'utf-8')) as {
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

    let deployTryCount = 1;
    let deployDone = false;
    do {
        try {
            await deployZip({
                webAppName,
                zipPath,
                userName: publishingUserName,
                // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
                password: publishingPassword!
            });
            deployDone = true;
        }
        catch (e) {
            if (deployTryCount >= 3)
                throw e;
            console.warn(e);
            deployTryCount += 1;
        }
    } while (!deployDone);

    console.log(`  Starting...`);
    await azureWebAppClient.webApps.start(resourceGroupName, webAppName);

    console.log(`  Done.`);
};