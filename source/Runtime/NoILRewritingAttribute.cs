using System;

namespace SharpLab.Runtime {
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class NoILRewritingAttribute : Attribute {
    }
}
