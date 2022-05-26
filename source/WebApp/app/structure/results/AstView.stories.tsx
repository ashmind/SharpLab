import React from 'react';
import { RecoilRoot } from 'recoil';
import { DarkModeRoot } from '../../shared/testing/DarkModeRoot';
import { AstView } from './AstView';

export default {
    component: AstView
};

type TemplateProps = React.ComponentProps<typeof AstView>;
const Template: React.FC<TemplateProps> = props => <RecoilRoot>
    <AstView {...props} />
</RecoilRoot>;

export const Full = () => <Template roots={[{
    type: 'node',
    kind: 'CompilationUnit',
    range: '0-10',
    children: [
        {
            type: 'operation',
            property: 'Operation',
            kind: 'MethodBodyOperation',
            properties: {}
        },
        {
            type: 'node',
            kind: 'GlobalStatement',
            range: '0-10',
            children: [
                {
                    type: 'node',
                    kind: 'LocalDeclarationStatement',
                    property: 'Statement',
                    range: '0-10',
                    children: [
                        {
                            type: 'operation',
                            property: 'Operation',
                            kind: 'VariableDeclarationGroup',
                            properties: {}
                        },
                        {
                            type: 'node',
                            kind: 'VariableDeclaration',
                            property: 'Declaration',
                            range: '0-9',
                            children: [
                                {
                                    type: 'operation',
                                    property: 'Operation',
                                    kind: 'VariableDeclaration',
                                    properties: {}
                                },
                                {
                                    type: 'node',
                                    kind: 'IdentifierName',
                                    property: 'Type',
                                    range: '0-4',
                                    children: [
                                        {
                                            type: 'token',
                                            kind: 'IdentifierToken',
                                            property: 'Identifier',
                                            range: '0-4',
                                            children: [
                                                {
                                                    type: 'value',
                                                    value: 'var',
                                                    range: '0-3'
                                                },
                                                {
                                                    type: 'trivia',
                                                    kind: 'WhitespaceTrivia',
                                                    range: '3-4',
                                                    value: ' '
                                                }
                                            ]
                                        }
                                    ]
                                },
                                {
                                    type: 'node',
                                    kind: 'VariableDeclarator',
                                    range: '4-9',
                                    children: [
                                        {
                                            type: 'operation',
                                            property: 'Operation',
                                            kind: 'VariableDeclarator',
                                            properties: {
                                                Symbol: 'x'
                                            }
                                        },
                                        {
                                            type: 'token',
                                            kind: 'IdentifierToken',
                                            property: 'Identifier',
                                            range: '4-6',
                                            children: [
                                                {
                                                    type: 'value',
                                                    value: 'x',
                                                    range: '4-5'
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
                                            kind: 'EqualsValueClause',
                                            property: 'Initializer',
                                            range: '6-9',
                                            children: [
                                                {
                                                    type: 'operation',
                                                    property: 'Operation',
                                                    kind: 'VariableInitializer',
                                                    properties: {
                                                        Locals: '<skipped>'
                                                    }
                                                },
                                                {
                                                    type: 'token',
                                                    kind: 'EqualsToken',
                                                    property: 'EqualsToken',
                                                    range: '6-8',
                                                    children: [
                                                        {
                                                            type: 'value',
                                                            value: '=',
                                                            range: '6-7'
                                                        },
                                                        {
                                                            type: 'trivia',
                                                            kind: 'WhitespaceTrivia',
                                                            range: '7-8',
                                                            value: ' '
                                                        }
                                                    ]
                                                },
                                                {
                                                    type: 'node',
                                                    kind: 'NumericLiteralExpression',
                                                    property: 'Value',
                                                    range: '8-9',
                                                    children: [
                                                        {
                                                            type: 'operation',
                                                            property: 'Operation',
                                                            kind: 'Literal',
                                                            properties: {
                                                                ConstantValue: '1',
                                                                Type: 'int'
                                                            }
                                                        },
                                                        {
                                                            type: 'token',
                                                            kind: 'NumericLiteralToken',
                                                            property: 'Token',
                                                            range: '8-9',
                                                            value: '1'
                                                        }
                                                    ]
                                                }
                                            ]
                                        }
                                    ]
                                }
                            ]
                        },
                        {
                            type: 'token',
                            kind: 'SemicolonToken',
                            property: 'SemicolonToken',
                            range: '9-10',
                            value: ';'
                        }
                    ]
                }
            ]
        },
        {
            type: 'token',
            kind: 'EndOfFileToken',
            property: 'EndOfFileToken',
            range: '10-10',
            value: ''
        }
    ]
}]} initialState={{ expanded: true }} />;
export const FullDarkMode = () => <DarkModeRoot><Full /></DarkModeRoot>;