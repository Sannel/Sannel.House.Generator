using Sannel.House.Generator.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Sannel.House.Generator.Common
{
	public class XUnitTestBuilder : ITestBuilder
	{
		public string[] Namespaces
		{
			get
			{
				return new String[]{ "Xunit"};
			}
		}

		public ExpressionSyntax Equal(ExpressionSyntax expected, ExpressionSyntax actual)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "Equal")
				).AddArgumentListArguments(
					Argument(expected),
					Argument(actual)
				);
		}

		public ExpressionSyntax Equal(ExpressionSyntax expected, ExpressionSyntax actual, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "Equal")
				).AddArgumentListArguments(
					Argument(expected),
					Argument(actual)
				);
		}

		public ExpressionSyntax NotEqual(ExpressionSyntax expected, ExpressionSyntax actual)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "NotEqual")
				).AddArgumentListArguments(
					Argument(expected),
					Argument(actual)
				);
		}

		public ExpressionSyntax NotEqual(ExpressionSyntax expected, ExpressionSyntax actual, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "NotEqual")
				).AddArgumentListArguments(
					Argument(expected),
					Argument(actual)
				);
		}

		public ExpressionSyntax False(ExpressionSyntax expression)
		{
			return InvocationExpression(
				Extensions.MemberAccess("Assert", "False")
			).AddArgumentListArguments(
				Argument(expression)
			);
		}

		public ExpressionSyntax False(ExpressionSyntax expression, string message)
		{
			return InvocationExpression(
				Extensions.MemberAccess("Assert", "False")
			).AddArgumentListArguments(
				Argument(expression),
				Argument(message.ToLiteral())
			);
		}

		public ExpressionSyntax NotNull(ExpressionSyntax expression)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "NotNull")
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax NotNull(ExpressionSyntax expression, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "NotNull")
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax Null(ExpressionSyntax expression)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "Null")
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax Null(ExpressionSyntax expression, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "Null")
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax True(ExpressionSyntax expression)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "True")
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax True(ExpressionSyntax expression, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "True")
				).AddArgumentListArguments(
					Argument(expression),
					Argument(message.ToLiteral())
				);
		}

		public AttributeSyntax GetClassAttribute()
		{
			return null;
		}

		public AttributeSyntax GetMethodAttribute()
		{
			return Attribute(IdentifierName("Fact"));
		}

		public ExpressionSyntax ThrowsAsync(TypeSyntax type, ExpressionSyntax expression)
		{
			return InvocationExpression(
					MemberAccessExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName("Assert"),
						GenericName("ThrowsAsync")
						.AddTypeArgumentListArguments(
							type
						)
					)
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax ThrowsAsync<T>(ExpressionSyntax expression)
		{
			var type = typeof(T);
			return InvocationExpression(
					MemberAccessExpression(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName("Assert"),
						GenericName("ThrowsAsync")
						.AddTypeArgumentListArguments(
							type.GetTypeSyntax()
						)
					)
				).AddArgumentListArguments(
					Argument(expression)
				);
		}
	}
}
