using Microsoft.Extensions.Logging;
using MirrorSharp.Advanced;
using SharpLab.Container.Manager.Internal;
using SharpLab.Server.Caching.Internal;
using SharpLab.Server.Execution.Container;
using SourceMock;

[assembly: GenerateMocksForTypes(
    typeof(IWorkSession),
    typeof(IRoslynSession),
    typeof(IDateTimeProvider),
    typeof(ILogger<>),
    typeof(IResultCacheStore),
    typeof(IContainerClient)
)]