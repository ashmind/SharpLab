import path from 'path';
import fs from 'fs-extra';
import getStream from 'get-stream';
import { BlobServiceClient } from '@azure/storage-blob';
import useAzure from '../helpers/useAzure';
import safeFetch from '../helpers/safeFetch';
import { getAzureCredentialsForAudience } from './getAzureCredentials';

const languageFeatureMapUrl = 'https://raw.githubusercontent.com/dotnet/roslyn/main/docs/Language%20Feature%20Status.md';
const branchesJsonFileName = 'branches.json';

async function getRoslynBranchFeatureMap(buildRoot: string) {
    const markdown = await (await safeFetch(languageFeatureMapUrl)).text();
    const languageVersions = markdown.matchAll(/#\s*(?<language>.+)\s*$\s*(?<table>(?:^\|.+$\s*)+)/gm);

    const mapPath = `${buildRoot}/RoslynFeatureMap.json`;
    let map = {} as Record<string, { language: string; name: string; url: string }|undefined>;
    if (await fs.pathExists(mapPath))
        map = await fs.readJson(mapPath);

    for (const languageMatch of languageVersions) {
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        const { language, table } = languageMatch.groups!;
        const rows = table.matchAll(/^\|(?<rawName>[^|]+)\|.+roslyn\/tree\/(?<branch>[A-Za-z\d\-/]+)/gm);

        for (const rowMatch of rows) {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            const { rawName, branch } = rowMatch.groups!;
            let name = rawName.trim();
            let url = '';
            const link = name.match(/\[([^\]]+)\]\(([^)]+)\)/);
            if (link)
                ([, name, url] = link);

            map[branch] = { language, name, url };
        }
    }

    await fs.writeFile(mapPath, JSON.stringify(map, null, 2));
    return map;
}

async function updateInAzureBlob(branch: {
    id: string;
    name: string;
    group: string;
    kind: string;
    url: string;
    feature?: {
        language: string;
        name: string;
        url: string;
    };
    commits: Array<{
        date: string;
        message: string;
        author: string;
        hash: string;
    }>;
}, branchesJsonArtifactPath: string) {
    const blobServiceUrl = 'https://slbs.blob.core.windows.net';
    const credentials = await getAzureCredentialsForAudience(blobServiceUrl);
    const blobServiceClient = new BlobServiceClient(
        blobServiceUrl,
        {
            async getToken() {
                const response = await credentials.getToken();
                return {
                    token: response.accessToken,
                    expiresOnTimestamp: (response.expiresOn as Date).getTime() / 1000
                };
            }
        }
    );
    const blobClient = blobServiceClient.getContainerClient('public').getBlockBlobClient('branches.json');

    console.log(`Downloading current ${branchesJsonFileName} from Azure...`);
    const download = await blobClient.download();
    console.log(`  ETag: ${download.etag ?? '<none>'}`);
    const branches = JSON.parse(
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        await getStream(download.readableStreamBody!)
    ) as Array<typeof branch>;

    const branchIndex = branches
        .map((branch, index) => ({ branch, index }))
        .find(x => x.branch.id === branch.id)
        ?.index;
    if (branchIndex) {
        branches.splice(branchIndex, 1, branch);
    }
    else {
        branches.push(branch);
    }

    const branchesJson = JSON.stringify(branches, null, 2);
    await fs.writeFile(branchesJsonArtifactPath, branchesJson);

    console.log(`Uploading updated ${branchesJsonFileName} to Azure...`);
    await blobClient.upload(branchesJson, branchesJson.length, {
        blobHTTPHeaders: {
            blobContentType: 'application/json',
            blobCacheControl: 'max-age=43200' // 12 hours
        },
        conditions: { ifMatch: download.etag }
    });
    console.log('  Done.');
}

export default async function updateInBranchesJson(
    { branch, buildRoot }: {
        branch: {
            name: string;
            id: string;
            url: string;
            commits: Array<{
                date: string;
                message: string;
                author: string;
                hash: string;
            }>;
        };
        buildRoot: string;
    }
) {
    const roslynBranchFeatureMap = await getRoslynBranchFeatureMap(buildRoot);
    const feature = roslynBranchFeatureMap[branch.name];
    const branchJson = {
        id: branch.id,
        name: branch.name,
        group: 'Roslyn branches',
        kind: 'roslyn',
        url: branch.url,
        ...(feature ? { feature } : {}),
        commits: branch.commits
    };

    const branchesJsonArtifactPath = path.join(buildRoot, branchesJsonFileName);
    if (useAzure) {
        await updateInAzureBlob(branchJson, branchesJsonArtifactPath);
    }
    else {
        // TODO: migrate to TypeScript
        throw new Error('Not migrated to TypeScript yet.');
    }
}