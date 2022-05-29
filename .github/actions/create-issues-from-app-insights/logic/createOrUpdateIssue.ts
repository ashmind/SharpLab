import type { Octokit } from '@octokit/rest';

type Issue = Awaited<ReturnType<Octokit['issues']['list']>>['data'][number];
type Context = {
    data: {
        title: string;
        body: string;
        comment: string;
    };

    label: string;
    labelCannotReproduce: string;

    issues: ReadonlyArray<Issue>;
    octokit: Octokit;
    owner: string;
    repo: string;
};

const findCreateOrReopenIssue = async ({
    data,

    label,
    labelCannotReproduce,

    issues,
    octokit,
    owner,
    repo
}: Context) => {
    console.log(`  ${data.title}`);

    const existing = issues.filter(i => i.title === data.title);
    if (existing.length > 1)
        throw new Error(`Found multiple issues with title '${data.title}':\n${existing.map(i => ' - ' + i.html_url).join('\n')}`);

    if (existing.length === 0) {
        console.log('    - creating');
        const issue = (await octokit.issues.create({
            owner,
            repo,
            title: data.title,
            body: data.body,
            labels: [label]
        })).data;
        console.log(`    - ${issue.html_url}`);
        return issue;
    }

    const issue = existing[0];
    console.log(`    - found at ${issue.url}`);

    const isClosedAsNotReproducible = labelCannotReproduce
        && issue.state === 'CLOSED'
        && issue.labels.some(l => typeof l === 'object' && l.name === labelCannotReproduce);
    if (isClosedAsNotReproducible) {
        console.log('    - reopening');
        await octokit.issues.update({
            owner,
            repo,
            issue_number: issue.number,
            state: 'open'
        });
        console.log(`    - removing ${labelCannotReproduce}`);
        await octokit.issues.removeLabel({
            owner,
            repo,
            issue_number: issue.number,
            name: labelCannotReproduce
        });
    }

    return issue;
};

export const createOrUpdateIssue = async (context: Context) => {
    const issue = await findCreateOrReopenIssue(context);
    const { data, octokit, owner, repo } = context;
    console.log('    - commenting');
    const comment = (await octokit.issues.createComment({
        owner,
        repo,
        issue_number: issue.number,
        body: data.comment
    })).data;
    console.log(`    - ${comment.html_url}`);
};