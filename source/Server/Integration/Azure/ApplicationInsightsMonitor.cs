using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Metrics;
using MirrorSharp.Advanced;
using MirrorSharp.Internal;
using Newtonsoft.Json;
using SharpLab.Server.MirrorSharp;
using SharpLab.Server.Monitoring;

namespace SharpLab.Server.Integration.Azure {
    public class ApplicationInsightsMonitor : IMonitor {
        private static readonly ConcurrentDictionary<MonitorMetric, MetricIdentifier> _metricIdentifiers = new();

        private readonly TelemetryClient _client;
        private readonly string _webAppName;

        public ApplicationInsightsMonitor(TelemetryClient client, string webAppName) {
            _client = Argument.NotNull(nameof(client), client);
            _webAppName = Argument.NotNullOrEmpty(nameof(webAppName), webAppName);
        }

        public void Metric(MonitorMetric metric, double value) {
            var identifier = _metricIdentifiers.GetOrAdd(metric, static m => new(m.Namespace, m.Name));
            _client.GetMetric(identifier).TrackValue(value);
        }

        public void Exception(Exception exception, IWorkSession? session, IDictionary<string, string>? extras = null) {
            var sessionInternals = session as WorkSession;
            var telemetry = new ExceptionTelemetry(exception) {
                Context = { Session = { Id = session?.GetSessionId() } },
                Properties = {
                    { "Web App", _webAppName },
                    { "Code", session?.GetText() },
                    { "Language", session?.LanguageName },
                    { "Target", session?.GetTargetName() },
                    { "Cursor", sessionInternals?.CursorPosition.ToString() },
                    { "Completion", FormatCompletion(sessionInternals) }
                }
            };
            if (extras != null) {
                foreach (var pair in extras) {
                    telemetry.Properties.Add(pair.Key, pair.Value);
                }
            }
            _client.TrackException(telemetry);
        }

        private string? FormatCompletion(WorkSession? session) {
            try {
                if (session == null)
                    return null;

                var current = session.CurrentCompletion;
                if (current.List == null && !current.ChangeEchoPending && current.PendingChar == null)
                    return null;

                return JsonConvert.ToString(new {
                    List = current.List is { } list ? new {
                        Items = new {
                            Take10 = list.Items.Take(10),
                            Length = list.Items.Length
                        },
                        list.Span
                    } : null,
                    current.ChangeEchoPending,
                    current.PendingChar
                });
            }
            catch (Exception ex) {
                return "<Failed to format completion: " + ex.ToString() + ">";
            }
        }
    }
}
