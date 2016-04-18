using System;
using System.Collections.Generic;
using System.Linq;
using AshMind.Extensions;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace TryRoslyn.Web.Formatting {
    /*public class SyntaxTreeConverter : JsonConverter {
        public override bool CanConvert(Type objectType) {
            return objectType.IsSubclassOf<SyntaxTree>();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            throw new NotSupportedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            WriteJson(writer, (SyntaxTree)value, serializer);
        }

        private void WriteJson(JsonWriter writer, SyntaxTree tree, JsonSerializer serializer) {
            WriteJsonRecursive(writer, tree.GetRoot());
        }

        private void WriteJsonRecursive(JsonWriter writer, SyntaxNode node) {
            writer.WriteStartObject();
            writer.WritePropertyName("kind");
            writer.WriteValue(node.Kind().ToString());
            writer.WritePropertyName("nodes");
            writer.WriteStartArray();
            foreach (var child in node.ChildNodes()) {
                WriteJsonRecursive(writer, child);
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }*/
}