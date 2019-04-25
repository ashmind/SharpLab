using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SharpYaml.Serialization;
using SourcePath;
using SourcePath.Roslyn;
using SharpLab.Server.Monitoring;

namespace SharpLab.Server.Explanation.Internal {
    public class ExternalSyntaxExplanationProvider : ISyntaxExplanationProvider, IDisposable {
        private readonly Func<HttpClient> _httpClientFactory;
        private readonly ExternalSyntaxExplanationSettings _settings;

        private IReadOnlyCollection<SyntaxExplanation> _explanations;
        private readonly SemaphoreSlim _explanationsLock = new SemaphoreSlim(1);

        private Task _updateTask;
        private CancellationTokenSource _updateCancellationSource;

        private readonly Serializer _serilializer = new Serializer(new SerializerSettings {
            NamingConvention = new FlatNamingConvention()
        });
        private readonly IMonitor _monitor;
        private readonly ISourcePathParser<RoslynNodeContext> _sourcePathParser;

        public ExternalSyntaxExplanationProvider(
            Func<HttpClient> httpClientFactory,
            ExternalSyntaxExplanationSettings settings,
            ISourcePathParser<RoslynNodeContext> sourcePathParser,
            IMonitor monitor
        ) {
            _httpClientFactory = httpClientFactory;
            _settings = settings;
            _sourcePathParser = sourcePathParser;
            _monitor = monitor;
        }

        public async ValueTask<IReadOnlyCollection<SyntaxExplanation>> GetExplanationsAsync(CancellationToken cancellationToken) {
            if (_explanations == null) {
                try {
                    await _explanationsLock.WaitAsync(cancellationToken).ConfigureAwait(false);
                    if (_explanations != null)
                        return _explanations;
                    _explanations = await LoadExplanationsSlowAsync(cancellationToken).ConfigureAwait(false);
                    _updateCancellationSource = new CancellationTokenSource();
                    _updateTask = Task.Run(UpdateLoopAsync);
                }
                finally {
                    _explanationsLock.Release();
                }
            }

            return _explanations;
        }

        private async Task<IReadOnlyCollection<SyntaxExplanation>> LoadExplanationsSlowAsync(CancellationToken cancellationToken) {
            var explanations = new List<SyntaxExplanation>();
            var serializer = new Serializer();
            using (var client = _httpClientFactory()) {
                var response = await client.GetAsync(_settings.SourceUrl, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var yamlString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var yaml = _serilializer.Deserialize<IEnumerable<YamlExplanation>>(yamlString);
                foreach (var item in yaml) {
                    SyntaxExplanation parsed;
                    try {
                        parsed = ParseExplanation(item);
                    }
                    catch (Exception ex) {
                        // depending on SourcePath version, it's possible that
                        // an explanation fails to parse on some branches
                        _monitor.Exception(ex, session: null);
                        continue;
                    }
                    explanations.Add(parsed);
                }
            }
            return explanations;
        }

        private SyntaxExplanation ParseExplanation(YamlExplanation item) {
            ISourcePath<RoslynNodeContext> path;
            try {
                path = _sourcePathParser.Parse(item.Path);
            }
            catch (Exception ex) {
                throw new Exception($"Failed to parse path for '{item.Name}': {ex.Message}.", ex);
            }
            return new SyntaxExplanation(path, item.Name, item.Text, item.Link);
        }

        private async Task UpdateLoopAsync() {
            while (!_updateCancellationSource.IsCancellationRequested) {
                try {
                    await Task.Delay(_settings.UpdatePeriod, _updateCancellationSource.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException) {
                    return;
                }
                try {
                    _explanations = await LoadExplanationsSlowAsync(_updateCancellationSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _monitor.Exception(ex, session: null);
                    // intentionally not re-throwing -- retrying after delay
                }
            }
        }

        public void Dispose() {
            DisposeAsync().Wait(TimeSpan.FromMinutes(1));
        }

        public async Task DisposeAsync() {
            using (_updateCancellationSource) {
                if (_updateTask == null)
                    return;
                _updateCancellationSource.Cancel();
                await _updateTask.ConfigureAwait(true);
            }
        }

        private class YamlExplanation {
            public string Name { get; set; }
            public string Text { get; set; }
            public string Link { get; set; }
            public string Path { get; set; }
        }
    }
}
