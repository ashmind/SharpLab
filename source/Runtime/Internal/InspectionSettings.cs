using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpLab.Runtime.Internal {
    public static class InspectionSettings {
        public static int CurrentProcessId { get; set; }
        public static ulong StackStart { get; set; }
    }
}
