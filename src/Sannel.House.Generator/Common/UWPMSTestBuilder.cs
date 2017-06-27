using Sannel.House.Generator.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Sannel.House.Generator.Common
{
	public class UWPMSTestBuilder : ITestBuilder
	{
		public string[] Namespaces
		{
			get
			{
				return new String[]
				{
					"Microsoft.VisualStudio.TestPlatform.UnitTestFramework"
				};
			}
		}

		public ExpressionSyntax Equal(ExpressionSyntax expected, ExpressionSyntax actual)
		{
			return InvocationExpression(
					Extensions.MemberAccess(
						IdentifierName("Assert"),
						IdentifierName("AreEqual")
					)
				).AddArgumentListArguments(
					Argument(expected),
					Argument(actual)
				);
		}

		public ExpressionSyntax Equal(ExpressionSyntax expected, ExpressionSyntax actual, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess(
						IdentifierName("Assert"),
						IdentifierName("AreEqual")
					)
				).AddArgumentListArguments(
					Argument(expected),
					Argument(actual),
					Argument(message.ToLiteral())
				);
		}

		public ExpressionSyntax NotEqual(ExpressionSyntax expected, ExpressionSyntax actual)
		{
			return InvocationExpression(
					Extensions.MemberAccess(
						IdentifierName("Assert"),
						IdentifierName("AreNotEqual")
					)
				).AddArgumentListArguments(
					Argument(expected),
					Argument(actual)
				);
		}

		public ExpressionSyntax NotEqual(ExpressionSyntax expected, ExpressionSyntax actual, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess(
						IdentifierName("Assert"),
						IdentifierName("AreNotEqual")
					)
				).AddArgumentListArguments(
					Argument(expected),
					Argument(actual),
					Argument(message.ToLiteral())
				);
		}

		public ExpressionSyntax False(ExpressionSyntax expression)
		{
			return InvocationExpression(
					Extensions.MemberAccess(
						IdentifierName("Assert"),
						IdentifierName("IsFalse")
					)
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax False(ExpressionSyntax expression, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess(
						IdentifierName("Assert"),
						IdentifierName("IsFalse")
					)
				).AddArgumentListArguments(
					Argument(expression),
					Argument(message.ToLiteral())
				);
		}

		public ExpressionSyntax NotNull(ExpressionSyntax expression)
		{
			return InvocationExpression(
					Extensions.MemberAccess(
						IdentifierName("Assert"),
						IdentifierName("IsNotNull")
					)
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax NotNull(ExpressionSyntax expression, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess(
						IdentifierName("Assert"),
						IdentifierName("IsNotNull")
					)
				).AddArgumentListArguments(
					Argument(expression),
					Argument(message.ToLiteral())
				);
		}

		public ExpressionSyntax Null(ExpressionSyntax expression)
		{
			return InvocationExpression(
					Extensions.MemberAccess(
						IdentifierName("Assert"),
						IdentifierName("IsNull")
					)
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax Null(ExpressionSyntax expression, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess(
						IdentifierName("Assert"),
						IdentifierName("IsNull")
					)
				).AddArgumentListArguments(
					Argument(expression),
					Argument(message.ToLiteral())
				);
		}

		public ExpressionSyntax True(ExpressionSyntax expression)
		{
			return InvocationExpression(
					Extensions.MemberAccess(
						IdentifierName("Assert"),
						IdentifierName("IsTrue")
					)
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax True(ExpressionSyntax expression, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess(
						IdentifierName("Assert"),
						IdentifierName("IsTrue")
					)
				).AddArgumentListArguments(
					Argument(expression),
					Argument(message.ToLiteral())
				);
		}

		public AttributeSyntax GetClassAttribute()
		{
			return Attribute(IdentifierName("TestClass"));
		}

		public AttributeSyntax GetMethodAttribute()
		{
			return Attribute(IdentifierName("TestMethod"));
		}

		public ExpressionSyntax ThrowsAsync(TypeSyntax type, ExpressionSyntax expression)
		{
			throw new NotImplementedException();
		}

		public ExpressionSyntax ThrowsAsync<T>(ExpressionSyntax expression)
		{
			throw new NotImplementedException();
		}
	}
}
