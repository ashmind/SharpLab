import React from 'react';
import { DarkModeRoot } from '../../../shared/testing/DarkModeRoot';
import { AstNode } from './AstNode';

export default {
    component: AstNode
};

export const Node = () => <AstNode item={{ type: 'node', value: 'CompilationUnit' }} />;
export const NodeDarkMode = () => <DarkModeRoot><Node /></DarkModeRoot>;
export const Token = () => <AstNode item={{ type: 'token', value: 'EndOfFileToken' }} />;
export const TokenDarkMode = () => <DarkModeRoot><Token /></DarkModeRoot>;
export const Value = () => <AstNode item={{ type: 'value', value: 'public' }} />;
export const ValueDarkMode = () => <DarkModeRoot><Value /></DarkModeRoot>;
export const Trivia = () => <AstNode item={{ type: 'trivia', value: ' ', kind: 'WhitespaceTrivia' }} />;
export const TriviaDarkMode = () => <DarkModeRoot><Trivia /></DarkModeRoot>;
export const TriviaMultiSpace = () => <AstNode item={{ type: 'trivia', value: '    ' }} />;
export const TriviaEscapeChars = () => <AstNode item={{ type: 'trivia', value: '\t \r\n' }} />;
export const Operation = () => <AstNode item={{ type: 'operation', value: 'Block', property: 'Operation' }} />;
export const OperationDarkMode = () => <DarkModeRoot><Operation /></DarkModeRoot>;
export const OperationProperty = () => <div className="ast">
    <AstNode item={{
        type: 'operation', value: 'VariableDeclarator', property: 'Operation',
        properties: {
            Symbol: 'i'
        }
    }} initialState={{ expanded: true }} />
</div>;
export const OperationPropertyDarkMode = () => <DarkModeRoot><OperationProperty /></DarkModeRoot>;