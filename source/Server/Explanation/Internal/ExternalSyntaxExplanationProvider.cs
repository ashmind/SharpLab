using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using SharpLab.Server.Monitoring;
using SharpYaml.Serialization;

namespace SharpLab.Server.Explanation.Internal {
    public class ExternalSyntaxExplanationProvider : ISyntaxExplanationProvider, IDisposable {
        private readonly Func<HttpClient> _httpClientFactory;
        private readonly Uri _sourceUrl;

        private IReadOnlyDictionary<SyntaxKind, SyntaxExplanation> _explanations;
        private readonly SemaphoreSlim _explanationsLock = new SemaphoreSlim(1);

        private Task _updateTask;
        private CancellationTokenSource _updateCancellationSource;
        private readonly TimeSpan _updatePeriod;

        private readonly Serializer _serilializer = new Serializer(new SerializerSettings {
            NamingConvention = new FlatNamingConvention()
        });
        private readonly IMonitor _monitor;

        public ExternalSyntaxExplanationProvider(Func<HttpClient> httpClientFactory, Uri sourceUrl, TimeSpan updatePeriod, IMonitor monitor) {
            _httpClientFactory = httpClientFactory;
            _sourceUrl = sourceUrl;
            _updatePeriod = updatePeriod;
            _monitor = monitor;
        }

        public async ValueTask<IReadOnlyDictionary<SyntaxKind, SyntaxExplanation>> GetExplanationsAsync(CancellationToken cancellationToken) {
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

        private async Task<IReadOnlyDictionary<SyntaxKind, SyntaxExplanation>> LoadExplanationsSlowAsync(CancellationToken cancellationToken) {
            var explanations = new Dictionary<SyntaxKind, SyntaxExplanation>();
            var serializer = new Serializer();
            using (var client = _httpClientFactory()) {
                var response = await client.GetAsync(_sourceUrl, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var yamlString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var yaml = _serilializer.Deserialize<IEnumerable<YamlExplanation>>(yamlString);
                foreach (var item in yaml) {
                    var explanation = new SyntaxExplanation(item.Name, item.Text, item.Link, item.Scope);
                    foreach (var kind in item.Match) {
                        explanations.Add(kind, explanation);
                    }
                }
            }
            return explanations;
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

            public SyntaxKind[] Match { get; set; }
            public SyntaxFragmentScope Scope { get; set; } = SyntaxFragmentScope.Self;
        }
    }
}
