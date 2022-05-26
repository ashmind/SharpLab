import React from 'react';
import { AstNodeList } from './AstNodeList';

export default {
    component: AstNodeList
};

export const Default = () => <AstNodeList items={[
    { type: 'node', value: 'Node 1' },
    { type: 'node', value: 'Node 2' }
]} />;