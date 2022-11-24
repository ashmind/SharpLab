import fs from 'fs-extra';
import { buildRootPath } from '../../../shared/paths';
import { safeFetch } from '../../../shared/safeFetch';
import type { Branch, Commit } from '../../../shared/types';
import { updateInBranchesJson } from '../../../shared/branchesJson';

const languageFeatureMapUrl = 'https://raw.githubusercontent.com/dotnet/roslyn/main/docs/Language%20Feature%20Status.md';

export async function getRoslynBranchFeatureMap() {
    const markdown = await (await safeFetch(languageFeatureMapUrl)).text();
    const languageVersions = markdown.matchAll(/#\s*(?<language>.+)\s*$\s*(?<table>(?:^\|.+$\s*)+)/gm);

    const mapPath = `${buildRootPath}/RoslynFeatureMap.json`;
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

export default async function publicBranchJson(branch: {
    id: string;
    name: string;
    url: string;
    commits: ReadonlyArray<Commit>;
}) {
    const roslynBranchFeatureMap = await getRoslynBranchFeatureMap();
    const feature = roslynBranchFeatureMap[branch.name];
    const branchJson = {
        id: branch.id,
        name: branch.name,
        group: 'Roslyn branches',
        kind: 'roslyn',
        url: branch.url,
        ...(feature ? { feature } : {}),
        commits: branch.commits,
        sharplab: {
            supportsUnknownOptions: true
        }
    } as Branch;

    await updateInBranchesJson(branchJson);
}