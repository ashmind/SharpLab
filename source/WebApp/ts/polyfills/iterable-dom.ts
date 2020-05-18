// Edge only, should be removed once it supports iterable DOM
// eslint-disable-next-line @typescript-eslint/no-unnecessary-condition
NodeList.prototype[Symbol.iterator] = NodeList.prototype[Symbol.iterator] || Array.prototype[Symbol.iterator];