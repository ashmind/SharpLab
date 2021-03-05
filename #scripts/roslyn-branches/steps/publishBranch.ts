import stream from 'stream';
import { promisify } from 'util';
import path from 'path';
import fs from 'fs-extra';
import got from 'got';
import delay from 'delay';
import { ResourceManagementClient } from '@azure/arm-resources';
import { WebSiteManagementClient } from '@azure/arm-appservice';
import AdmZip from 'adm-zip';
import dateFormat from 'dateformat';
import useAzure from './useAzure';
import getEnvForAzure from './getEnvForAzure';
import { getAzureCredentials } from './getAzureCredentials';

const pipeline = promisify(stream.pipeline);

const resourceGroupName = 'SharpLab';
const appServicePlanName = 'SharpLab-Main';

export default async function publishBranch({ webAppName, iisSiteName, webAppUrl, branchArtifactsRoot, branchSiteRoot }: {
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

        const formatStatus = ({ statusCode, statusMessage }: { statusCode: number; statusMessage?: string }) =>
            `  ${statusCode} ${statusMessage ?? ''}`;

        while (tryPermanent < 3 && tryTemporary < 30) {
            try {
                const response = await got(`${webAppUrl}/status`, { retry: 0 });
                ok = true;
                console.log(formatStatus(response));
                break;
            }
            catch (e) {
                // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
                if (e.response) {
                    // eslint-disable-next-line @typescript-eslint/no-unsafe-member-access
                    console.warn(formatStatus(e.response));
                }

                const temporary = (e instanceof got.HTTPError) && e.response.statusCode === 503;
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
        const telemetryKey = getEnvForAzure('SHARPLAB_TELEMETRY_KEY');
        console.log(`Deploying to Azure, ${webAppName}...`);

        const { credentials, subscriptionId } = await getAzureCredentials();

        const azureResourceClient = new ResourceManagementClient(credentials, subscriptionId);
        const azureWebAppClient = new WebSiteManagementClient(credentials, subscriptionId);

        const resourceGroup = await azureResourceClient.resourceGroups.get(resourceGroupName);
        const appServicePlan = await azureWebAppClient.appServicePlans.get(resourceGroupName, appServicePlanName);

        console.log(`  Creating or updating web app...`);
        const result = await azureWebAppClient.webApps.createOrUpdate(resourceGroupName, webAppName, {
            location: resourceGroup.location,
            serverFarmId: appServicePlan.id!,
            siteConfig: {
                webSocketsEnabled: true,
                appSettings: Object.entries({
                    SHARPLAB_WEBAPP_NAME: webAppName,
                    SHARPLAB_TELEMETRY_KEY: telemetryKey
                }).map(([name, value]) => ({ name, value }))
            }
        });
        console.log(`    Status: ${result._response.status}`);

        console.log(`  Zipping...`);
        const zipPath = path.join(branchArtifactsRoot, 'Site.zip');
        console.log(`    => ${zipPath}`);
        const zip = new AdmZip();
        zip.addLocalFolder(branchSiteRoot);
        zip.writeZip(zipPath);

        console.log(`  Publishing...`);
        const {
            publishingUserName,
            publishingPassword
        } = await azureWebAppClient.webApps.listPublishingCredentials(resourceGroupName, webAppName);

        console.log(`    ⏱️ ${dateFormat(new Date(), 'HH:MM:ss')}`);
        await pipeline(
            fs.createReadStream(zipPath),
            got.stream.post(`https://${webAppName}.scm.azurewebsites.net/api/zipdeploy`, {
                username: publishingUserName,
                password: publishingPassword
            })
        );
        console.log(`    ✔️ ${dateFormat(new Date(), 'HH:MM:ss')}`);

        console.log(`  Done.`);
    }

    if (useAzure) {
        await publishToAzure();
    }
    else {
        // TODO: migrate to TypeScript
        throw new Error('Not migrated to TypeScript yet.');
        /*await execa('powershell', [`${__dirname}/Publish-BranchToIIS.ps1`,
            '-SiteName', iisSiteName,
            '-SourcePath', branchSiteRoot
        ], {
            stdout: 'inherit',
            stderr: 'inherit'
        });*/
    }

    await testBranchWebApp();
}