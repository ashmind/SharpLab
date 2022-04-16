// @ts-expect-error 'createTransformer is nullable, but there is no easy way to make a non-null assertion in JS'
// eslint-disable-next-line @typescript-eslint/no-unsafe-call, @typescript-eslint/no-unsafe-member-access, @typescript-eslint/no-var-requires
module.exports = require('babel-jest').default.createTransformer({
    'presets': ['@babel/preset-env']
});