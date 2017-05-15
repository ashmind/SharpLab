using MirrorSharp;

namespace TryRoslyn.Server.MirrorSharp {
    public interface IMirrorSharpSetup {
        void ApplyTo(MirrorSharpOptions options);
    }
}