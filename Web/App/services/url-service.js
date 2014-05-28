angular.module('app').service('UrlService', ['$location', '$rootScope', function ($location, $rootScope) {
    'use strict';
    
    var lastHash;
    this.saveToUrl = function(data) {
        var hash = LZString.compressToBase64(data.code);
        var flags = stringifyOptions(data.options);
        if (flags)
            hash = 'f:' + flags + '/' + hash;

        if (data.branch)
            hash = 'b:' + data.branch + '/' + hash;

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
        var match = /(?:b:([^\/]+)\/)?(?:f:([^\/]+)\/)?(.+)/.exec(hash);
        if (match == null)
            return null;

        var result = { branch: match[1] };
        try {
            result.code = LZString.decompressFromBase64(match[3]);
        }
        catch (e) {
            return null;
        }

        result.options = parseOptions(match[2]);
        return result;
    }

    function stringifyOptions(options) {
        return [
            options.language === 'vbnet' ? 'vb' : '',
            options.target === 'vbnet' ? '>vb' : '',
            options.mode === 'script' ? 's' : '',
            options.optimizations ? 'r' : ''
        ].join('');
    }

    function parseOptions(flags) {
        if (!flags)
            return {};

        return {
            language:      /(^|[a-z])vb/.test(flags) ? 'vbnet'  : 'csharp',
            target:        />vb/.test(flags)         ? 'vbnet'  : 'csharp',
            mode:          flags.indexOf("s") > -1   ? 'script' : 'regular',
            optimizations: flags.indexOf("r") > -1
        };
    }
}]);