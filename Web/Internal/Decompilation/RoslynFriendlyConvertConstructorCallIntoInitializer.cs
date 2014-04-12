// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// Copyright (c) 2014 Andrey Shchekin (TryRsolyn changes only)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using Mono.Cecil;

namespace TryRoslyn.Web.Internal.Decompilation {
    // This is mostly a copy of ConvertConstructorCallIntoInitializer from Decompiler library.
    // However it is simplified so that it does not try to use field initializers, as that can not be
    // correctly represented for primary constructor decompilation.
    public class RoslynFriendlyConvertConstructorCallIntoInitializer : DepthFirstAstVisitor<object, object>, IAstTransform {
        public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data) {
            var stmt = constructorDeclaration.Body.Statements.FirstOrDefault() as ExpressionStatement;
            if (stmt == null)
                return null;

            var invocation = stmt.Expression as InvocationExpression;
            if (invocation == null)
                return null;

            var mre = invocation.Target as MemberReferenceExpression;
            if (mre == null || mre.MemberName != ".ctor")
                return null;

            var initializer = new ConstructorInitializer();
            if (mre.Target is ThisReferenceExpression)
                initializer.ConstructorInitializerType = ConstructorInitializerType.This;
            else if (mre.Target is BaseReferenceExpression)
                initializer.ConstructorInitializerType = ConstructorInitializerType.Base;
            else
                return null;

            // Move arguments from invocation to initializer:
            invocation.Arguments.MoveTo(initializer.Arguments);
            // Add the initializer: (unless it is the default 'base()')
            if (initializer.ConstructorInitializerType != ConstructorInitializerType.Base || initializer.Arguments.Count > 0) {
                initializer.AddAnnotation(invocation.Annotation<MethodReference>());
                constructorDeclaration.Initializer = initializer;
            }
            // Remove the statement:
            stmt.Remove();
            return null;
        }

        public override object VisitTypeDeclaration(TypeDeclaration typeDeclaration, object data) {
            // Now convert base constructor calls to initializers:
            base.VisitTypeDeclaration(typeDeclaration, data);

            // Remove single empty constructor:
            RemoveSingleEmptyConstructor(typeDeclaration);

            return null;
        }

        private void RemoveSingleEmptyConstructor(TypeDeclaration typeDeclaration) {
            var instanceCtors = typeDeclaration.Members.OfType<ConstructorDeclaration>().Where(c => (c.Modifiers & Modifiers.Static) == 0).ToArray();
            if (instanceCtors.Length != 1)
                return;

            var emptyCtor = new ConstructorDeclaration();
            emptyCtor.Modifiers = ((typeDeclaration.Modifiers & Modifiers.Abstract) == Modifiers.Abstract ? Modifiers.Protected : Modifiers.Public);
            emptyCtor.Body = new BlockStatement();
            if (emptyCtor.IsMatch(instanceCtors[0]))
                instanceCtors[0].Remove();
        }

        public void Run(AstNode node) {
            node.AcceptVisitor(this, null);
        }
    }
}