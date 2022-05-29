import { promises as fs } from 'fs';
import * as core from '@actions/core';
import { Octokit } from '@octokit/rest';
import { paginateRest } from '@octokit/plugin-paginate-rest';
import { createActionAuth } from '@octokit/auth-action';
import { createOrUpdateIssue } from './logic/createOrUpdateIssue.js';
import { queryAppInsights } from './logic/queryAppInsights.js';

const appInsightsQueryPath = core.getInput('app-insights-query-path', { required: true });
const appInsightsApps = core.getInput('app-insights-apps', { required: true });
// eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
const appInsightsPeriod = core.getInput('app-insights-period', { required: true });
const githubLabel = core.getInput('github-label', { required: true });
const githubCannotReproduceLabel = core.getInput('github-label-cannot-reproduce');

const octokit = new (Octokit.plugin(paginateRest))({
    authStrategy: createActionAuth
});

const appInsightsData = await queryAppInsights({
    query: await fs.readFile(appInsightsQueryPath, { encoding: 'utf-8' }),
    apps: appInsightsApps,
    period: appInsightsPeriod
});

console.log('Querying GitHub Issues');
// eslint-disable-next-line @typescript-eslint/no-non-null-assertion
const [owner, repo] = process.env.GITHUB_REPOSITORY!.split('/');
const issues = await octokit.paginate(
    'GET /repos/{owner}/{repo}/issues',
    {
        owner,
        repo,
        per_page: 100
    }
);

console.log('Processing data');
for (const data of appInsightsData) {
    await createOrUpdateIssue({
        data,
        issues,
        octokit,
        owner,
        repo,
        label: githubLabel,
        labelCannotReproduce: githubCannotReproduceLabel
    });
}