using System;
using System.IO;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using IL.Syntax;
using Mono.Cecil;
using Xunit;

namespace IL.Tests {
    public class DeclarationTests {
        [Theory]
        [InlineData(".class A {}")]
        public void Minimal(string code) {
            AssertRoundtrips(code);
        }

        [Theory]
        [InlineData(".class '<A>' {}")]
        public void EscapedIdentifiers(string code) {
            AssertRoundtrips(code);
        }

        [Theory]
        [InlineData(".class public A {}")]
        [InlineData(".class private A {}")]
        [InlineData(".class auto A {}")]
        [InlineData(".class ansi A {}")]
        [InlineData(".class private auto A {}")]
        public void Modifiers(string code) {
            AssertRoundtrips(code);
        }

        private void AssertRoundtrips(string code) {
            var parsed = TestHelper.Parse(code);
            Assert.Equal(code, parsed.ToString());
        }
    }
}
