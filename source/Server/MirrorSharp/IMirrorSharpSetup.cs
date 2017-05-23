using MirrorSharp;

namespace SharpLab.Server.MirrorSharp {
    public interface IMirrorSharpSetup {
        void SlowApplyTo(MirrorSharpOptions options);
    }
}