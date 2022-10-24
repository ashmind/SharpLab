using System;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;
using ProtoBuf;
using SharpLab.Container.Protocol.Stdin;

namespace SharpLab.Container.Manager.Internal {
    public class StdinWriter {
        private readonly ILogger<StdinWriter> _logger;

        public StdinWriter(ILogger<StdinWriter> logger) {
            _logger = logger;
        }

        public StdinWriterResult WriteCommand(CancellableInputStream stream, StdinCommand command, CancellationToken cancellationToken) {
            // Safe operation -- stream is associated with the container and is only vailable to one request at once
            stream.CancellationToken = cancellationToken;
            try {
                Serializer.SerializeWithLengthPrefix(stream, command, PrefixStyle.Base128);
                return StdinWriterResult.Success;
            }
            catch (IOException ex) {                
                _logger.LogInformation(ex, "Failed to write stream");
                return StdinWriterResult.Failure(FailureMessages.IOFailure);
            }
            catch (OperationCanceledException) {
                _logger.LogDebug("Timed out while writing stream");
                return StdinWriterResult.Failure(FailureMessages.TimedOut);
            }
            finally {
                stream.CancellationToken = null;
            }
        }
    }
}
