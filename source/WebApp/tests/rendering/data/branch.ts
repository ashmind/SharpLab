export const branch = {
    id: 'main',
    name: 'main',
    group: 'Roslyn branches',
    kind: 'roslyn',
    url: 'https://sl-b-dotnet-main.azurewebsites.net',
    commits: [{
        date: new Date('2020-05-22T18:57:31Z'),
        message: 'Merge pull request #44520 from sharwell/increase-timeouts\n\nIncrease timeouts to account for slower build machines',
        author: 'Joey Robichaud',
        hash: '287a25a51324f252e61b6f2186e7df60c8a6161b'
    }]
} as const;