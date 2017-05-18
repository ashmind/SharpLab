using MirrorSharp;

namespace SharpLab.Server.MirrorSharp {
    public interface IMirrorSharpSetup {
        void ApplyTo(MirrorSharpOptions options);
    }
}