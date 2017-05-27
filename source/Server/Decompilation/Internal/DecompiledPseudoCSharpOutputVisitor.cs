using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using JetBrains.Annotations;

namespace SharpLab.Server.Decompilation.Internal {
    public class DecompiledPseudoCSharpOutputVisitor : OverridableCSharpOutputVisitor {
        private bool _currentStatementIsNotValidCSharp = false;

        public DecompiledPseudoCSharpOutputVisitor(TextWriter textWriter, CSharpFormattingOptions formattingPolicy) : base(textWriter, formattingPolicy) {
        }

        public DecompiledPseudoCSharpOutputVisitor(IOutputFormatter formatter, CSharpFormattingOptions formattingPolicy) : base(formatter, formattingPolicy) {
        }

        // fixes bug https://github.com/ashmind/SharpLab/issues/7
        // todo: report this to the decompiler guys -- but does not seem like they are reading their queue
        public override void VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression) {
            StartNode(memberReferenceExpression);
            
            var useParentheses = RequiresParenthesesWhenTargetOfMemberReference(memberReferenceExpression.Target);
            if (useParentheses)
                LPar();
            memberReferenceExpression.Target.AcceptVisitor(this);
            if (useParentheses)
                RPar();

            WriteToken(Roles.Dot);
            WriteIdentifier(memberReferenceExpression.MemberName);
            WriteTypeArguments(memberReferenceExpression.TypeArguments);
            EndNode(memberReferenceExpression);
        }

        private bool RequiresParenthesesWhenTargetOfMemberReference(Expression expression) {
            return (expression is ConditionalExpression)
                || (expression is BinaryOperatorExpression)
                || (expression is UnaryOperatorExpression);
        }

        public override void VisitExpressionStatement(ExpressionStatement expressionStatement) {
            StartNode(expressionStatement);
            expressionStatement.Expression.AcceptVisitor(this);
            WriteToken(Roles.Semicolon);
            if (_currentStatementIsNotValidCSharp) {
                Space();
                VisitComment(new Comment(" This is not valid C#, but it represents the IL correctly."));

                _currentStatementIsNotValidCSharp = false;
            }
            NewLine();
            EndNode(expressionStatement);
        }

        public override void VisitInvocationExpression(InvocationExpression invocationExpression) {
            // writes base..ctor() as base() and this..ctor() as this()
            var memberReference = invocationExpression.Target as MemberReferenceExpression;
            if (IsBaseOrThisConstructor(memberReference)) {
                var keyword = (memberReference.Target is ThisReferenceExpression)
                            ? ConstructorInitializer.ThisKeywordRole
                            : ConstructorInitializer.BaseKeywordRole;

                StartNode(invocationExpression);
                WriteKeyword(keyword);
                Space(policy.SpaceBeforeMethodCallParentheses);
                WriteCommaSeparatedListInParenthesis(invocationExpression.Arguments, policy.SpaceWithinMethodCallParentheses);
                EndNode(invocationExpression);
                _currentStatementIsNotValidCSharp = true;
                return;
            }
            
            base.VisitInvocationExpression(invocationExpression);
        }

        [ContractAnnotation("memberReference:null => false")]
        private static bool IsBaseOrThisConstructor(MemberReferenceExpression memberReference) {
            return memberReference != null
                && (memberReference.Target is BaseReferenceExpression || memberReference.Target is ThisReferenceExpression)
                && memberReference.MemberName == ".ctor";
        }
    }
}