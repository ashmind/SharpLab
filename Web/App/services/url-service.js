angular.module('app').service('UrlService', ['$location', '$rootScope', function ($location, $rootScope) {
    var lastHash;
    this.saveToUrl = function(data) {
        var hash = LZString.compressToBase64(data.code);
        if (data.branch)
            hash = "b:" + data.branch + "/" + hash;

        lastHash = hash;
        $location.hash(hash);
    }

    this.loadFromUrl = load.bind(this, false);

    this.onUrlChange = function (callback) {
        $rootScope.$on('$locationChangeSuccess', function () {
            var loaded = load(true);
            if (loaded !== null)
                callback(loaded);
        });
    }

    function load(onlyIfChanged) {
        var hash = $location.hash();
        if (!hash)
            return null;

        hash = hash.replace(/^#/, '');
        if (!hash || (onlyIfChanged && hash === lastHash))
            return null;

        lastHash = hash;
        var match = /(?:b:([^\/]+)\/)(.+)/.exec(hash);
        if (match == null)
            return null;

        try {
            return {
                branch: match[1],
                code: LZString.decompressFromBase64(match[2])
            };
        }
        catch (e) {
            return null;
        }
    }
}]);