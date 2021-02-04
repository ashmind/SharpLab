import './env';
import stream from 'stream';
import path from 'path';
import { promisify } from 'util';
import fs from 'fs-extra';
import globby from 'globby';
import git from 'simple-git/promise';
import got from 'got';
import extract from 'extract-zip';
import execa from 'execa';
import chalk from 'chalk';
import useAzure from './steps/useAzure';
import publishBranch from './steps/publishBranch';
import updateInBranchesJson from './steps/updateInBranchesJson';

const pipeline = promisify(stream.pipeline);

const branchVersionFileName = 'branch-version.json';
const branchName = process.argv[2]
   ?? (() => { throw new Error('Branch name was not provided'); })();

const root = path.resolve(`${__dirname}/../..`);
const sourceRoot = path.join(root, 'source');
const buildRoot = path.join(root, '!roslyn-branches');
fs.ensureDirSync(buildRoot);

const branchFSName = 'dotnet-' + branchName.replace(/[/\\:_]/g, '-');
const branchRoot = path.join(buildRoot, branchFSName);
const branchArtifactsRoot = path.join(branchRoot, 'artifacts');
const branchSiteRoot = path.join(branchRoot, 'site');

let webAppName = `sl-b-${branchFSName.toLowerCase()}`;
if (webAppName.length > 60) {
    webAppName = webAppName.substr(0, 57) + '-01'; // no uniqueness check at the moment, we can add later
    console.warn(`Name is too long, using '${webAppName}'.`);
}

const iisSiteName = `${webAppName}.sharplab.local`;
const webAppUrl = useAzure
    ? `https://${webAppName}.azurewebsites.net`
    : `http://${iisSiteName}`;

console.log('Environment:');
console.log(`  Azure:                 ${useAzure}`);
console.log(`  Root:                  ${root}`);
console.log(`  Source Root:           ${sourceRoot}`);
console.log(`  Build Root:            ${buildRoot}`);
console.log(`  Branch FS Name:        ${branchFSName}`);
console.log(`  Branch Root:           ${branchRoot}`);
console.log(`  Branch Artifacts Root: ${branchArtifactsRoot}`);
console.log(`  Branch Site Root:      ${branchSiteRoot}`);
console.log(`  Web App Name:          ${webAppName}`);
console.log(`  Web App URL:           ${webAppUrl}`);
console.log('');

async function updateRoslynBuildPackages(currentBuildId: string|null) {
    const roslynBuildsUrl = `https://dev.azure.com/dnceng/public/_apis/build/builds?api-version=5.0&definitions=15&reasonfilter=individualCI&resultFilter=succeeded&$top=1&branchName=refs/heads/${branchName}`;
    const builds = (await got(roslynBuildsUrl).json<{
        count: number;
        value: ReadonlyArray<{
            id: string;
            sourceVersion: string;
            _links: {
                self: { href: string };
            };
        }>;
    }>());
    if (builds.count === 0)
        throw 'No successful Roslyn Azure builds found.';

    const build = builds.value[0];
    if (build.id === currentBuildId) {
        console.log(`Roslyn Azure build ${build.id} same as current, skipping.`);
        return false;
    }

    await fs.ensureDir(branchArtifactsRoot);
    const packagesRoot = path.join(branchArtifactsRoot, 'roslyn-packages');

    const result = {
        buildId: build.id,
        commitHash: build.sourceVersion,
        packagesRoot
    };

    const roslynArtifactsUrl = `${build._links.self.href}/artifacts`;
    console.log(`GET ${roslynArtifactsUrl}`);

    const roslynArtifacts = await got(roslynArtifactsUrl).json<{
        value: ReadonlyArray<{
            name: string;
            resource: {
                downloadUrl: string;
            };
        }>;
    }>();
    const roslynPackages = roslynArtifacts.value.find(a => a.name === 'Packages - PreRelease');
    if (!roslynPackages)
        throw 'Packages were not found in Roslyn Azure build artifacts.';

    const downloadUrl = roslynPackages.resource.downloadUrl;
    const zipPath = path.join(branchArtifactsRoot, `Packages.${build.id}.zip`);

    if (!(await fs.pathExists(zipPath))) { // Optimization for local only
        console.log(`GET ${downloadUrl} => ${zipPath}`);
        await pipeline(
            got.stream(downloadUrl),
            fs.createWriteStream(zipPath)
        );
    }
    else {
        console.log(`Found cached ${zipPath}, no need to download`);
    }

    if (await fs.pathExists(packagesRoot))
        await fs.remove(packagesRoot);
    await fs.mkdir(packagesRoot);

    // makes it easier to flatten subdirectories
    const packagesTempRoot = path.join(branchArtifactsRoot, 'roslyn-packages-temp');
    if (await fs.pathExists(packagesTempRoot))
        await fs.remove(packagesTempRoot);

    console.log(`Unpacking ${zipPath} => ${packagesTempRoot}`);
    await extract(zipPath, { dir: packagesTempRoot });

    console.log(`Flattening ${packagesTempRoot} => ${packagesRoot}`);
    for await (const filePath of globby.stream(['**/*.*'], { cwd: packagesTempRoot, absolute: true })) {
        const targetPath = path.join(packagesRoot, path.basename(filePath as string));
        await fs.copyFile(filePath as string, targetPath);
    }

    console.log(`Deleting ${packagesTempRoot}`);
    await fs.remove(packagesTempRoot);
    return result;
}

async function getSharpLabCommitHash() {
    return await git(sourceRoot).revparse(['HEAD']);
}

async function buildSharpLab(roslynPackagesRoot: string) {
    const runtime = 'win-x86';

    const branchSharpLabRoot = path.join(branchRoot, 'sharplab');
    const branchSourceRoot = path.join(branchSharpLabRoot, 'source');

    await fs.ensureDir(branchSourceRoot);

    console.log('Building Roslyn package map...');

    const roslynVersionMap = Object.fromEntries(
        (await globby(['*.nupkg'], { cwd: roslynPackagesRoot, absolute: true })).map(filePath => {
            // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
            const [, name, version] = path.basename(filePath).match(/^([^\d]+)\.(\d.+)\.nupkg$/)!;
            return [name, version];
        })
    );

    console.log(`Copying ${sourceRoot} => ${branchSourceRoot}`);
    await fs.copy(sourceRoot, branchSourceRoot, {
        filter: path => !/[/\\](?:obj|bin|node_modules|.vs|.git)(?:[/\\]|$)/.test(path)
    });

    const restoredPackagesRoot = path.join(branchSharpLabRoot, 'packages');
    console.log('Updating Roslyn package versions in projects...');
    for await (const projectPathUntyped of globby.stream(['**/*.csproj'], { cwd: branchSourceRoot, absolute: true })) {
        // sigh: dotnet.exe should do this, but of course it does not

        const projectPath = projectPathUntyped as string;
        const projectName = path.basename(projectPath);

        let projectXml = await fs.readFile(projectPath, { encoding: 'utf-8' });
        let changed = false;
        projectXml = projectXml.replace(/(<PackageReference\s+Include="([^"]+)"\s+Version=")([^"]+)/g, (m, beforeVersion: string, id: string, currentVersion: string) => {
            const roslynVersion = roslynVersionMap[id];
            if (!roslynVersion || roslynVersion === currentVersion)
                return m;

            if (!changed)
                console.log(`  ${projectName}`);

            console.log(`    ${id}: ${currentVersion} => ${roslynVersion}`);
            changed = true;
            return `${beforeVersion}${roslynVersion}`;
        });

        // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
        if (changed)
            await fs.writeFile(projectPath, projectXml);
    }

    // Important because all Roslyn builds seem to use the exact same version
    console.log('Deleting older Roslyn packages');
    for (const packageName of Object.keys(roslynVersionMap)) {
        const packagePath = path.join(restoredPackagesRoot, packageName);
        if (await fs.pathExists(packagePath)) {
            console.log(`  ${packagePath}`);
            await fs.remove(packagePath);
        }
    }

    console.log(`Restoring ${roslynPackagesRoot} => ${restoredPackagesRoot}`);
    await execa('dotnet', [
        'restore', branchSourceRoot,
        '--runtime', runtime,
        '--packages', restoredPackagesRoot,
        '--source', 'https://api.nuget.org/v3/index.json',
        '--source', 'https://dotnet.myget.org/F/symreader-converter/api/v3/index.json',
        '--source', 'https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet5/nuget/v3/index.json',
        '--source', roslynPackagesRoot,
        '--verbosity', 'minimal'
    ], {
        stdout: 'inherit',
        stderr: 'inherit'
    });

    console.log(`Building SharpLab`);
    await execa('dotnet', [
        'msbuild', `${branchSourceRoot}/Server/Server.csproj`,
        '/m', '/nodeReuse:false',
        '/t:Publish',
        '/p:SelfContained=True',
        '/p:AspNetCoreHostingModel=OutOfProcess',
        `/p:RuntimeIdentifier=${runtime}`,
        '/p:Configuration=Release',
        '/p:UnbreakablePolicyReportEnabled=false',
        '/p:TreatWarningsAsErrors=false'
    ], {
        stdout: 'inherit',
        stderr: 'inherit'
    });

    return {
        publishRoot: `${branchSourceRoot}/Server/bin/Release/net5.0/${runtime}/publish`
    };
}

async function getBranchVersionFromWebApp() {
    try {
        const currentVersion = await got(`${webAppUrl}/${branchVersionFileName}`).json<{
            roslyn: { buildId: string };
            sharplab: { commitHash: string };
        }>();
        console.log(JSON.stringify(currentVersion));
        return currentVersion;
    }
    catch (e) {
        // eslint-disable-next-line @typescript-eslint/restrict-template-expressions
        console.warn(`Failed to get branch version from ${webAppUrl}/${branchVersionFileName}:\r\n  ${e}`);
        return {
            roslyn: { buildId: null },
            sharplab: { commitHash: null }
        };
    }
}

async function updateBranchVersionArtifact(roslynBuild: { buildId: string; commitHash: string }, sourceCommitHash: string) {
    const newVersion = {
        name: branchName,
        roslyn: {
            buildId: roslynBuild.buildId,
            commitHash: roslynBuild.commitHash
        },
        sharplab: {
            commitHash: sourceCommitHash
        }
    };
    console.log(JSON.stringify(newVersion));

    const versionArtifactPath = path.join(branchArtifactsRoot, branchVersionFileName);
    await fs.writeFile(versionArtifactPath, JSON.stringify(newVersion));
    return versionArtifactPath;
}

async function getBranchInfo(commitHash: string) {
    const { commit } = await got(`https://api.github.com/repos/dotnet/roslyn/commits/${commitHash}`).json<{
        commit: {
            author: {
                name: string;
                date: string;
            };
            message: string;
        };
    }>();
    return {
        id: branchFSName.replace(/^dotnet-/, ''),
        name: branchName,
        url: webAppUrl,
        repository: 'dotnet',
        commits: [{
            date: commit.author.date,
            message: commit.message,
            author: commit.author.name,
            hash: commitHash
        }]
    };
}

async function buildBranch() {
    console.log('Getting branch version from Web App...');
    const currentVersion = await getBranchVersionFromWebApp();
    console.log('');

    console.log('Comparing SharpLab version...');
    const sourceCommitHash = await getSharpLabCommitHash();
    const sourceChanged = sourceCommitHash !== currentVersion.sharplab.commitHash;
    console.log(`  old hash: ${currentVersion.sharplab.commitHash ?? '<none>'}`);
    console.log(`  new hash: ${sourceCommitHash}`);
    console.log(`  changed:  ${sourceChanged}`);
    console.log('');

    console.log('Downloading Roslyn Azure build...');
    // if source hash changed, we need to redownload to rebuild anyways (as GitHub actions will not cache in FS)
    const roslynBuildIdToCheck = !sourceChanged ? currentVersion.roslyn.buildId : null;
    const roslynBuild = await updateRoslynBuildPackages(roslynBuildIdToCheck);
    if (!roslynBuild) {
        // nothing has changed
        return { built: false } as const;
    }
    console.log('');

    console.log('Updating and building SharpLab...');
    const siteSource = await buildSharpLab(roslynBuild.packagesRoot);
    console.log('');

    console.log('Copying to site');
    await fs.copy(siteSource.publishRoot, branchSiteRoot);

    console.log('Updating branch version for Web App');
    const branchVersionPath = await updateBranchVersionArtifact(roslynBuild, sourceCommitHash);
    const branchSiteContentRoot = path.join(branchSiteRoot, 'wwwroot');
    await fs.ensureDir(branchSiteContentRoot);
    await fs.copy(branchVersionPath, path.join(branchSiteContentRoot, branchVersionFileName));

    console.log('Preparing branch info');
    const info = await getBranchInfo(roslynBuild.commitHash);

    return { built: true, info } as const;
}

async function run() {
    console.log(chalk.white('* Building'));
    const buildResult = await buildBranch();
    if (!buildResult.built) {
        // Up-to-date
        return;
    }
    console.log('');

    console.log(chalk.white('* Publishing'));
    await publishBranch({
        webAppName,
        iisSiteName,
        webAppUrl,
        branchSiteRoot,
        branchArtifactsRoot
    });

    console.log(chalk.white('* Listing'));
    await updateInBranchesJson({
        buildRoot,
        branch: buildResult.info
    });
}

run().catch(e => {
    console.error("::error::" + e);
    process.exit(1);
});