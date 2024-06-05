import path from 'path';
import fs from 'fs-extra';
import getStream from 'get-stream';
import { type BlobDownloadResponseParsed, BlobServiceClient } from '@azure/storage-blob';
import { getAzureCredential } from './getAzureCredential';
import { useAzure } from './useAzure';
import type { Branch } from './types';
import { buildRootPath } from './paths';

const branchesJsonFileName = 'branches.json';

const getBranchesJsonBlobClient = () => {
    const blobServiceUrl = 'https://slbs.blob.core.windows.net';
    const credential = getAzureCredential();
    const blobServiceClient = new BlobServiceClient(blobServiceUrl, credential);
    return blobServiceClient.getContainerClient('public')
        .getBlockBlobClient(branchesJsonFileName);
};

const parseBranchesFromDownload = async (download: BlobDownloadResponseParsed) => {
    return JSON.parse(
        // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
        await getStream(download.readableStreamBody!)
    ) as Array<Branch>;
};

export const getBranchesJson = async (): Promise<ReadonlyArray<Branch>> => {
    const blobClient = getBranchesJsonBlobClient();
    const download = await blobClient.download();

    return parseBranchesFromDownload(download);
};

async function updateInAzureBlob(branch: Branch, branchesJsonArtifactPath: string) {
    const blobClient = getBranchesJsonBlobClient();

    console.log(`Downloading current ${branchesJsonFileName} from Azure...`);
    const download = await blobClient.download();
    console.log(`  ETag: ${download.etag ?? '<none>'}`);
    const branches = await parseBranchesFromDownload(download);

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