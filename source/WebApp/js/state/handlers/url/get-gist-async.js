import languages from '../../../helpers/languages.js';

const languageMap = {
    'C#':           languages.csharp,
    'Visual Basic': languages.vb,
    'F#':           languages.fsharp
};

export default async function getGistAsync(id) {
    const gist = await (await fetch(`https://api.github.com/gists/${id}`)).json();
    const file = Object.values(gist.files)[0];
    return {
        id,
        language: languageMap[file.language] || languages.csharp,
        code: file.content
    };
}