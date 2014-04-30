angular.module('app').service('UrlService', ['$location', '$rootScope', function ($location, $rootScope) {
    var lastHash;
    this.saveToUrl = function(value) {
        var hash = LZString.compressToBase64(value);
        lastHash = hash;
        $location.hash(hash);
    }

    this.loadFromUrl = load.bind(this, false);

    this.onUrlChange = function (callback) {
        $rootScope.$on('$locationChangeSuccess', function () {
            var value = load(true);
            if (value !== null)
                callback(value);
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
        try {
            return LZString.decompressFromBase64(hash);
        }
        catch (e) {
            return null;
        }
    }
}]);