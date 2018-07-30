export default Object.freeze({
    run: {
        csharp: [
            '// Run mode:',
            '//   value.Inspect()      — often better than Console.WriteLine',
            '//   Inspect.Heap(object) — structure of an object in memory (heap)',
            '//   Inspect.Stack(value) — structure of a stack value'
        ].join('\r\n')
    }
});