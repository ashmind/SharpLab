using System;
using System.Collections.Generic;

namespace SharpLab.Runtime.Internal {
    [Serializable]
    public class MemoryInspectionResult {
        public MemoryInspectionResult(ulong address, string title, IReadOnlyList<Field> fields, byte[] data) {
            Address = address;
            Title = title;
            Fields = fields;
            Data = data;
        }

        public ulong Address { get; }
        public string Title { get; }
        public IReadOnlyList<Field> Fields { get; }
        public byte[] Data { get; }

        [Serializable]
        public readonly struct Field {
            public Field(string name, int offset, int size) {
                Name = name;
                Offset = offset;
                Size = size;
            }

            public string Name { get; }
            public int Offset { get; }
            public int Size { get; }
        }
    }
}
