import path from 'path';
import fs from 'fs-extra';
import getStream from 'get-stream';
import { BlobServiceClient } from '@azure/storage-blob';
import { getAzureCredential } from './getAzureCredential';
import { useAzure } from './useAzure';
import type { Branch } from './types';
import { buildRootPath } from './paths';
import { safeFetch } from './safeFetch';

const branchesJsonFileName = 'branches.json';

export const getBranchesJson = async () => await (await safeFetch(
    'https://slbs.azureedge.net/public/branches.json'
)).json() as ReadonlyArray<Branch>;

async function updateInAzureBlob(branch: Branch, branchesJsonArtifactPath: string) {
    const blobServiceUrl = 'https://slbs.blob.core.windows.net';
    const credential = await getAzureCredential();
    const blobServiceClient = new BlobServiceClient(blobServiceUrl, credential/*
        {
            async getToken() { return credential.getToken(blobServiceUrl); }
        }*/
    );
    const blobClient = blobServiceClient.getContainerClient('public').getBlockBlobClient('branches.json');

    console.log(`Downloading current ${branchesJsonFileName} from Azure...`);
    const download = await blobClient.download();
    console.log(`  ETag: ${download.etag ?? '<none>'}`);
    const branches = JSON.parse(
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        await getStream(download.readableStreamBody!)
    ) as Array<Branch>;

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

export async function updateInBranchesJson(branch: Branch) {
    const branchesJsonArtifactPath = path.join(buildRootPath, branchesJsonFileName);
    if (useAzure) {
        await updateInAzureBlob(branch, branchesJsonArtifactPath);
    }
    else {
        // TODO: migrate to TypeScript
        throw new Error('Not migrated to TypeScript yet.');
    }
}