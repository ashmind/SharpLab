import { execa } from 'execa';

type Column = { name: string };

export const queryAppInsights = async ({
    query,
    apps,
    period
}: {
    query: string;
    apps: string;
    period: string;
}) => {
    console.log('Querying App Insights');
    const { columns, rows } = (JSON.parse((await execa('az', [
        'monitor', 'app-insights', 'query',
        '--analytics-query', query.replace(/[\r\n]+/g, ' '),
        '--apps', apps,
        '--offset', period
    ], {
        stderr: process.stderr
    })).stdout) as {
        tables: ReadonlyArray<{
            columns: ReadonlyArray<Column>;
            rows: ReadonlyArray<ReadonlyArray<string>>;
        }>;
    }).tables[0];

    const columnIndexOf = (name: string) => {
        const index = columns.findIndex(c => c.name === name);
        if (index < 0)
            throw new Error(`Could not find column '${name}' in App Insights output. Found columns: ${columns.map(c => c.name).join(', ')}.`);
        return index;
    };

    const titleColumnIndex = columnIndexOf('title');
    const bodyColumnIndex = columnIndexOf('body');
    const commentColumnIndex = columnIndexOf('comment');

    return rows.map(r => ({
        title: r[titleColumnIndex],
        body: r[bodyColumnIndex],
        comment: r[commentColumnIndex]
    }));
};