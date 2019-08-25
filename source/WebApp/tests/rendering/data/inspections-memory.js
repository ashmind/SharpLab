export const string = {
    type: 'inspection:memory',
    title: 'System.String at 0x2E9DA9E8',
    labels: [
        {
            name: 'header',
            offset: 0,
            length: 4,
            levelSpan: 1
        },
        {
            name: 'type handle',
            offset: 4,
            length: 4,
            levelSpan: 1
        },
        {
            name: 'm_stringLength',
            offset: 8,
            length: 4,
            levelSpan: 1
        },
        {
            name: 'm_firstChar',
            offset: 12,
            length: 2,
            levelSpan: 1
        }
    ],
    data: [
        0,
        0,
        0,
        128,
        96,
        253,
        13,
        113,
        4,
        0,
        0,
        0,
        97,
        0,
        98,
        0,
        99,
        0,
        100,
        0,
        0,
        0
    ]
};


export const nested = {
    type: 'inspection:memory',
    title: 'Nested',
    labels: [
        {
            name: 'a',
            offset: 0,
            length: 4,
            levelSpan: 3
        },
        {
            name: 'b',
            offset: 4,
            length: 8,
            nested: [
                {
                    name: 'ba',
                    offset: 4,
                    length: 4,
                    levelSpan: 2
                },
                {
                    name: 'bb',
                    offset: 8,
                    length: 4,
                    nested: [
                        {
                            name: 'bba',
                            offset: 8,
                            length: 4,
                            levelSpan: 1
                        }
                    ]
                }
            ]
        },
        {
            name: 'c',
            offset: 12,
            length: 4,
            levelSpan: 3
        }
    ],
    data: [
        1,
        0,
        0,
        0,
        2,
        0,
        0,
        0,
        3,
        0,
        0,
        0,
        4,
        0,
        0,
        0
    ]
};