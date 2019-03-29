using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using MirrorSharp.Advanced;
using SharpLab.Server.MirrorSharp;
using SharpLab.Server.Monitoring;

namespace SharpLab.Server.Azure {
    public class ApplicationInsightsMonitor : IMonitor {
        private readonly TelemetryClient _client;
        private readonly string _webAppName;

        public ApplicationInsightsMonitor(TelemetryClient client, string webAppName) {
            _client = Argument.NotNull(nameof(client), client);
            _webAppName = Argument.NotNullOrEmpty(nameof(webAppName), webAppName);
        }

        public void Event(string name, IWorkSession session, IDictionary<string, string> extras = null) {
            var telemetry = new EventTelemetry(name);
            AddDefaultDetails(telemetry, session, extras);
            _client.TrackEvent(telemetry);
        }

        public void Exception(Exception exception, IWorkSession session, IDictionary<string, string> extras = null) {
            var telemetry = new ExceptionTelemetry(exception) {
                Properties = {
                    { "Code", session?.GetText() }
                }
            };
            AddDefaultDetails(telemetry, session, extras);
            _client.TrackException(telemetry);
        }

        private void AddDefaultDetails<TTelemetry>(TTelemetry telemetry, IWorkSession session, IDictionary<string, string> extras)
            where TTelemetry: ITelemetry, ISupportProperties
        {
            telemetry.Context.Session.Id = session?.GetSessionId();
            telemetry.Properties.Add("Web App", _webAppName);
            if (extras == null)
                return;

            foreach (var pair in extras) {
                telemetry.Properties.Add(pair.Key, pair.Value);
            }
        }
    }
}
