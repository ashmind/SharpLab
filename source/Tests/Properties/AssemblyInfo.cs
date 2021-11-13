using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using MirrorSharp.Advanced;
using SharpLab.Container.Manager.Internal;
using SharpLab.Server.Caching.Internal;
using SharpLab.Server.Execution.Container;
using SharpLab.Server.Monitoring;
using SourceMock;

[assembly: GenerateMocksForTypes(
    typeof(IWorkSession),
    typeof(IRoslynSession),
    typeof(IDateTimeProvider),
    typeof(IMonitor),
    typeof(ILogger<>),
    typeof(IResultCacheStore),
    typeof(IContainerClient),
    typeof(BlobContainerClient)
)]