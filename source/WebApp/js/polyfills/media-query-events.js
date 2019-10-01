// Edge only, should be removed once it supports addEventListener on MediaQueryList
MediaQueryList.prototype.addEventListener = MediaQueryList.prototype.addEventListener || function(type, listener) {
    if (type !== 'change')
        return;
    this.addListener(listener);
};