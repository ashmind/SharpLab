export default [
    {
        type: 'node',
        kind: 'CompilationUnit',
        range: '0-64',
        children: [
            {
                type: 'node',
                kind: 'UsingDirective',
                range: '0-15',
                children: [
                    {
                        type: 'token',
                        kind: 'UsingKeyword',
                        property: 'UsingKeyword',
                        range: '0-6',
                        children: [
                            {
                                type: 'value',
                                value: 'using',
                                range: '0-5'
                            },
                            {
                                type: 'trivia',
                                kind: 'WhitespaceTrivia',
                                range: '5-6',
                                value: ' '
                            }
                        ]
                    },
                    {
                        type: 'node',
                        kind: 'IdentifierName',
                        property: 'Name',
                        range: '6-12',
                        children: [
                            {
                                type: 'token',
                                kind: 'IdentifierToken',
                                property: 'Identifier',
                                range: '6-12',
                                value: 'System'
                            }
                        ]
                    },
                    {
                        type: 'token',
                        kind: 'SemicolonToken',
                        property: 'SemicolonToken',
                        range: '12-15',
                        children: [
                            {
                                type: 'value',
                                value: ';',
                                range: '12-13'
                            },
                            {
                                type: 'trivia',
                                kind: 'EndOfLineTrivia',
                                range: '13-15',
                                value: '\r\n'
                            }
                        ]
                    }
                ]
            },
            {
                type: 'node',
                kind: 'ClassDeclaration',
                range: '15-64',
                children: [
                    {
                        type: 'token',
                        kind: 'PublicKeyword',
                        range: '15-22',
                        children: [
                            {
                                type: 'value',
                                value: 'public',
                                range: '15-21'
                            },
                            {
                                type: 'trivia',
                                kind: 'WhitespaceTrivia',
                                range: '21-22',
                                value: ' '
                            }
                        ]
                    },
                    {
                        type: 'token',
                        kind: 'ClassKeyword',
                        property: 'Keyword',
                        range: '22-28',
                        children: [
                            {
                                type: 'value',
                                value: 'class',
                                range: '22-27'
                            },
                            {
                                type: 'trivia',
                                kind: 'WhitespaceTrivia',
                                range: '27-28',
                                value: ' '
                            }
                        ]
                    },
                    {
                        type: 'token',
                        kind: 'IdentifierToken',
                        property: 'Identifier',
                        range: '28-30',
                        children: [
                            {
                                type: 'value',
                                value: 'C',
                                range: '28-29'
                            },
                            {
                                type: 'trivia',
                                kind: 'WhitespaceTrivia',
                                range: '29-30',
                                value: ' '
                            }
                        ]
                    },
                    {
                        type: 'token',
                        kind: 'OpenBraceToken',
                        property: 'OpenBraceToken',
                        range: '30-33',
                        children: [
                            {
                                type: 'value',
                                value: '{',
                                range: '30-31'
                            },
                            {
                                type: 'trivia',
                                kind: 'EndOfLineTrivia',
                                range: '31-33',
                                value: '\r\n'
                            }
                        ]
                    },
                    {
                        type: 'node',
                        kind: 'MethodDeclaration',
                        range: '33-63',
                        children: [
                            {
                                type: 'operation',
                                property: 'Operation',
                                kind: 'MethodBody',
                                properties: {}
                            },
                            {
                                type: 'token',
                                kind: 'PublicKeyword',
                                range: '33-44',
                                children: [
                                    {
                                        type: 'trivia',
                                        kind: 'WhitespaceTrivia',
                                        range: '33-37',
                                        value: '    '
                                    },
                                    {
                                        type: 'value',
                                        value: 'public',
                                        range: '37-43'
                                    },
                                    {
                                        type: 'trivia',
                                        kind: 'WhitespaceTrivia',
                                        range: '43-44',
                                        value: ' '
                                    }
                                ]
                            },
                            {
                                type: 'node',
                                kind: 'PredefinedType',
                                property: 'ReturnType',
                                range: '44-49',
                                children: [
                                    {
                                        type: 'token',
                                        kind: 'VoidKeyword',
                                        property: 'Keyword',
                                        range: '44-49',
                                        children: [
                                            {
                                                type: 'value',
                                                value: 'void',
                                                range: '44-48'
                                            },
                                            {
                                                type: 'trivia',
                                                kind: 'WhitespaceTrivia',
                                                range: '48-49',
                                                value: ' '
                                            }
                                        ]
                                    }
                                ]
                            },
                            {
                                type: 'token',
                                kind: 'IdentifierToken',
                                property: 'Identifier',
                                range: '49-50',
                                value: 'M'
                            },
                            {
                                type: 'node',
                                kind: 'ParameterList',
                                property: 'ParameterList',
                                range: '50-53',
                                children: [
                                    {
                                        type: 'token',
                                        kind: 'OpenParenToken',
                                        property: 'OpenParenToken',
                                        range: '50-51',
                                        value: '('
                                    },
                                    {
                                        type: 'token',
                                        kind: 'CloseParenToken',
                                        property: 'CloseParenToken',
                                        range: '51-53',
                                        children: [
                                            {
                                                type: 'value',
                                                value: ')',
                                                range: '51-52'
                                            },
                                            {
                                                type: 'trivia',
                                                kind: 'WhitespaceTrivia',
                                                range: '52-53',
                                                value: ' '
                                            }
                                        ]
                                    }
                                ]
                            },
                            {
                                type: 'node',
                                kind: 'Block',
                                property: 'Body',
                                range: '53-63',
                                children: [
                                    {
                                        type: 'operation',
                                        property: 'Operation',
                                        kind: 'Block',
                                        properties: {
                                            Locals: '<skipped>'
                                        }
                                    },
                                    {
                                        type: 'token',
                                        kind: 'OpenBraceToken',
                                        property: 'OpenBraceToken',
                                        range: '53-56',
                                        children: [
                                            {
                                                type: 'value',
                                                value: '{',
                                                range: '53-54'
                                            },
                                            {
                                                type: 'trivia',
                                                kind: 'EndOfLineTrivia',
                                                range: '54-56',
                                                value: '\r\n'
                                            }
                                        ]
                                    },
                                    {
                                        type: 'token',
                                        kind: 'CloseBraceToken',
                                        property: 'CloseBraceToken',
                                        range: '56-63',
                                        children: [
                                            {
                                                type: 'trivia',
                                                kind: 'WhitespaceTrivia',
                                                range: '56-60',
                                                value: '    '
                                            },
                                            {
                                                type: 'value',
                                                value: '}',
                                                range: '60-61'
                                            },
                                            {
                                                type: 'trivia',
                                                kind: 'EndOfLineTrivia',
                                                range: '61-63',
                                                value: '\r\n'
                                            }
                                        ]
                                    }
                                ]
                            }
                        ]
                    },
                    {
                        type: 'token',
                        kind: 'CloseBraceToken',
                        property: 'CloseBraceToken',
                        range: '63-64',
                        value: '}'
                    }
                ]
            },
            {
                type: 'token',
                kind: 'EndOfFileToken',
                property: 'EndOfFileToken',
                range: '64-64',
                value: ''
            }
        ]
    }
];