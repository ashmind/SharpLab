using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MirrorSharp.Advanced;

namespace SharpLab.Server.Decompilation.Internal
{
    public static class FastJsonWriterExtensions {
        public static void WriteValueFromParts(this IFastJsonWriter writer, string part1, string part2, string part3) {
            using (var stringWriter = writer.OpenString()) {
                stringWriter.Write(part1);
                stringWriter.Write(part2);
                stringWriter.Write(part3);
            }
        }

        public static void WriteValueFromParts(this IFastJsonWriter writer, string part1, char part2, string part3) {
            using (var stringWriter = writer.OpenString()) {
                stringWriter.Write(part1);
                stringWriter.Write(part2);
                stringWriter.Write(part3);
            }
        }

        public static void WriteValueFromParts(this IFastJsonWriter writer, int part1, char part2, int part3) {
            using (var stringWriter = writer.OpenString()) {
                stringWriter.Write(part1);
                stringWriter.Write(part2);
                stringWriter.Write(part3);
            }
        }

        public static void WriteValueFromParts(this IFastJsonWriter writer, int part1, char part2, int part3, char part4, int part5, char part6, int part7) {
            using (var stringWriter = writer.OpenString()) {
                stringWriter.Write(part1);
                stringWriter.Write(part2);
                stringWriter.Write(part3);
                stringWriter.Write(part4);
                stringWriter.Write(part5);
                stringWriter.Write(part6);
                stringWriter.Write(part7);
            }
        }
    }
}
