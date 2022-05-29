import React from 'react';
import { Diagnostic } from './Diagnostic';

export default {
    component: Diagnostic
};

export const Warning = () => <Diagnostic data={{ severity: 'warning', id: 'CS0219', message: "The variable 'x' is assigned but its value is never used" }} />;
export const Error = () => <Diagnostic data={{ severity: 'error', id: 'CS1525', message: "Invalid expression term ';'" }} />;
export const ServerError = () => <Diagnostic data={{ message: 'Unexpected sever error' }} />;