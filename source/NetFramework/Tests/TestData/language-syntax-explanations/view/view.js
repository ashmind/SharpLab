(async () => {
    const body = document.getElementsByTagName('body')[0];
    const title = (document.getElementsByTagName('title')[0]).innerText;
    const yamlUrl = body.dataset.source;

    const template = await (await fetch('view/view.template.html')).text();
    const yaml = await (await fetch(yamlUrl)).text();

    const items = jsyaml.safeLoad(yaml);
    items.sort((a, b) => {
        if (a.name > b.name) return +1;
        if (a.name < b.name) return -1;
        return 0;
    });
    const render = vash.compile(template);

    vash.helpers.markdown = s => vash.helpers.raw(marked(s));
    body.innerHTML = render({
        title,
        items,
        linkText: u => u.replace(/^https:\/\/docs.microsoft.com\/en-us\/dotnet\/csharp\//, '')
    });

    setTimeout(() => {
        const hash = window.location.hash;
        window.location.hash = '';
        window.location.hash = hash;
    }, 0);
})();