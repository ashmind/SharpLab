angular.module('app').service('UrlService', ['$location', '$rootScope', 'Modes', function ($location, $rootScope, modes) {
    var lastHash;
    this.saveToUrl = function(data) {
        var hash = LZString.compressToBase64(data.code);
        if (data.mode === modes.script)
            hash = 's/' + hash;

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
        var match = /(?:b:([^\/]+)\/)?(s\/)?(.+)/.exec(hash);
        if (match == null)
            return null;

        try {
            return {
                branch: match[1],
                mode: match[2] ? modes.script : modes.regular,
                code: LZString.decompressFromBase64(match[3])
            };
        }
        catch (e) {
            return null;
        }
    }
}]);