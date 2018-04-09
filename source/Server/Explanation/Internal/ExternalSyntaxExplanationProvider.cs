using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SharpYaml.Serialization;
using SourcePath.CSharp;
using SharpLab.Server.Monitoring;

namespace SharpLab.Server.Explanation.Internal {
    public class ExternalSyntaxExplanationProvider : ISyntaxExplanationProvider, IDisposable {
        private readonly Func<HttpClient> _httpClientFactory;
        private readonly Uri _sourceUrl;

        private IReadOnlyCollection<SyntaxExplanation> _explanations;
        private readonly SemaphoreSlim _explanationsLock = new SemaphoreSlim(1);

        private Task _updateTask;
        private CancellationTokenSource _updateCancellationSource;
        private readonly TimeSpan _updatePeriod;

        private readonly Serializer _serilializer = new Serializer(new SerializerSettings {
            NamingConvention = new FlatNamingConvention()
        });
        private readonly IMonitor _monitor;
        private readonly ISyntaxPathParser _syntaxPathParser;

        public ExternalSyntaxExplanationProvider(
            Func<HttpClient> httpClientFactory,
            Uri sourceUrl,
            TimeSpan updatePeriod,
            IMonitor monitor,
            ISyntaxPathParser syntaxPathParser
        ) {
            _httpClientFactory = httpClientFactory;
            _sourceUrl = sourceUrl;
            _updatePeriod = updatePeriod;
            _monitor = monitor;
            _syntaxPathParser = syntaxPathParser;
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
                var response = await client.GetAsync(_sourceUrl, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var yamlString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var yaml = _serilializer.Deserialize<IEnumerable<YamlExplanation>>(yamlString);
                foreach (var item in yaml) {
                    explanations.Add(ParseExplanation(item));
                }
            }
            return explanations;
        }

        private SyntaxExplanation ParseExplanation(YamlExplanation item) {
            SyntaxPath path;
            try {
                path = _syntaxPathParser.Parse(item.Path, SyntaxPathAxis.DescendantOrSelf);
            }
            catch (Exception ex) {
                throw new Exception($"Failed to parse path for '{item.Name}': {ex.Message}.", ex);
            }
            return new SyntaxExplanation(path, item.Name, item.Text, item.Link);
        }

        private async Task UpdateLoopAsync() {
            while (!_updateCancellationSource.IsCancellationRequested) {
                try {
                    await Task.Delay(_updatePeriod, _updateCancellationSource.Token).ConfigureAwait(false);
                }
                catch (TaskCanceledException) {
                    return;
                }
                try {
                    _explanations = await LoadExplanationsSlowAsync(_updateCancellationSource.Token).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    _monitor.Exception(ex, null);
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
