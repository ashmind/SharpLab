import { useAzure } from '../../../shared/useAzure';
import { publishToAzure } from './publish/publishToAzure';
import { testWebApp } from './publish/testWebApp';

export const publishBranch = async ({ webAppName, webAppUrl, branchArtifactsRoot, branchSiteRoot }: {
    webAppName: string;
    iisSiteName: string;
    webAppUrl: string;
    branchArtifactsRoot: string;
    branchSiteRoot: string;
}) => {
    if (useAzure) {
        await publishToAzure({
            webAppName,
            branchArtifactsRoot,
            branchSiteRoot
        });
    }
    else {
        // TODO: migrate to TypeScript
        throw new Error('Not migrated to TypeScript yet.');
    }

    await testWebApp({ url: webAppUrl });
};