import execa, { ExecaSyncError } from 'execa';
import { task, exec } from 'oldowan';
import groupBy from 'object.groupby';

type NpmDependencies = {
    readonly [name: string]: NpmPackage;
};

type NpmPackage = {
    readonly name: string;
    readonly path: string;
    readonly dependencies?: NpmDependencies;
};
type PackageResult = Pick<NpmPackage, 'name' | 'path'>;

const unused = task('deps:unused', () => exec('depcheck'));

// TODO: Not currently correct -- needs to be refined for
// CodeMirror scenarios specifically
// eslint-disable-next-line @typescript-eslint/no-unused-vars
const duplicates = task('deps:duplicates', async () => {
    const result = (await execa('npm', ['ls', '-all', '-json', '-long'], {
        reject: false
    }));
    if (result.failed && result.exitCode === 0)
        throw new Error(`npm ls failed: ${(result as unknown as ExecaSyncError).shortMessage}\n${result.stderr}`);

    const root = JSON.parse(result.stdout) as NpmPackage;
    const narrow = ({ name, path }: NpmPackage) => ({ name, path });
    const all = (function* traverse(dependencies: NpmDependencies): Iterable<PackageResult> {
        for (const _package of Object.values(dependencies)) {
            yield narrow(_package);
            if (_package.dependencies)
                yield* traverse(_package.dependencies);
        }
    })(root.dependencies!);
    const grouped = Object
        .values(groupBy(all, p => p.name))
        .map(group => ({
            name: group[0].name,
            paths: Object.keys(groupBy(group, g => g.path))
        }));

    const duplicates = Object
        .values(grouped)
        .filter(g => g.paths.length > 1);

    if (duplicates.length === 0)
        return;

    console.error(`Found duplicate dependencies.`);
    for (const group of duplicates) {
        console.error(`  ${group.name}:`);
        for (const path of group.paths) {
            console.error(`    ${path}`);
        }
    }

    throw new Error('Found duplicate dependencies.');
});

export const deps = task('deps', () => Promise.all([
    unused() //,
    // duplicates()
]));