export const branch = {
    id: 'master',
    name: 'master',
    group: 'Roslyn branches',
    kind: 'roslyn',
    url: 'https://sl-b-dotnet-master.azurewebsites.net',
    commits: [{
        date: new Date('2020-05-22T18:57:31Z'),
        message: 'Merge pull request #44520 from sharwell/increase-timeouts\n\nIncrease timeouts to account for slower build machines',
        author: 'Joey Robichaud',
        hash: '287a25a51324f252e61b6f2186e7df60c8a6161b'
    }]
} as const;