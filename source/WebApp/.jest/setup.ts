import { TextEncoder, TextDecoder } from 'util';
import { Crypto } from '@peculiar/webcrypto';

Object.defineProperty(global, 'crypto', {
    value: new Crypto()
});

global.TextDecoder = TextDecoder as typeof global.TextDecoder;
global.TextEncoder = TextEncoder;