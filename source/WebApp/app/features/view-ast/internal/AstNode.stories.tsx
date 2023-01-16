import React from 'react';
import { darkModeStory } from '../../../shared/testing/darkModeStory';
import { AstNode } from './AstNode';

export default {
    component: AstNode
};

export const Node = () => <AstNode item={{ type: 'node', value: 'CompilationUnit' }} />;
export const NodeDarkMode = darkModeStory(Node);
export const Token = () => <AstNode item={{ type: 'token', value: 'EndOfFileToken' }} />;
export const TokenDarkMode = darkModeStory(Token);
export const Value = () => <AstNode item={{ type: 'value', value: 'public' }} />;
export const ValueDarkMode = darkModeStory(Value);
export const Trivia = () => <AstNode item={{ type: 'trivia', value: ' ', kind: 'WhitespaceTrivia' }} />;
export const TriviaDarkMode = darkModeStory(Trivia);
export const TriviaMultiSpace = () => <AstNode item={{ type: 'trivia', value: '    ' }} />;
export const TriviaEscapeChars = () => <AstNode item={{ type: 'trivia', value: '\t \r\n' }} />;
export const Operation = () => <AstNode item={{ type: 'operation', value: 'Block', property: 'Operation' }} />;
export const OperationDarkMode = darkModeStory(Operation);
export const OperationProperty = () => <div className="ast">
    <AstNode item={{
        type: 'operation',
        value: 'VariableDeclarator',
        property: 'Operation',
        properties: {
            Symbol: 'i'
        }
    }} initialState={{ expanded: true }} />
</div>;
export const OperationPropertyDarkMode = darkModeStory(OperationProperty);
export const CollapsedWithChildren = () => <div className="ast">
    <AstNode item={{
        type: 'node',
        value: 'CompilationUnit',
        children: [
            { type: 'node', value: 'ClassDeclaration' }
        ]
    }} />
</div>;