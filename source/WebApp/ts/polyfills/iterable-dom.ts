// Edge only, should be removed once it supports iterable DOM
NodeList.prototype[Symbol.iterator] = NodeList.prototype[Symbol.iterator] || Array.prototype[Symbol.iterator];