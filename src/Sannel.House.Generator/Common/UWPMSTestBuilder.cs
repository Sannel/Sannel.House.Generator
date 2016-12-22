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

		public ExpressionSyntax AssertAreEqual(ExpressionSyntax expected, ExpressionSyntax actual)
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

		public ExpressionSyntax AssertAreEqual(ExpressionSyntax expected, ExpressionSyntax actual, string message)
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

		public ExpressionSyntax AssertAreNotEqual(ExpressionSyntax expected, ExpressionSyntax actual)
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

		public ExpressionSyntax AssertAreNotEqual(ExpressionSyntax expected, ExpressionSyntax actual, string message)
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

		public ExpressionSyntax AssertIsFalse(ExpressionSyntax expression)
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

		public ExpressionSyntax AssertIsFalse(ExpressionSyntax expression, string message)
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

		public ExpressionSyntax AssertIsNotNull(ExpressionSyntax expression)
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

		public ExpressionSyntax AssertIsNotNull(ExpressionSyntax expression, string message)
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

		public ExpressionSyntax AssertIsNull(ExpressionSyntax expression)
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

		public ExpressionSyntax AssertIsNull(ExpressionSyntax expression, string message)
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

		public ExpressionSyntax AssertIsTrue(ExpressionSyntax expression)
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

		public ExpressionSyntax AssertIsTrue(ExpressionSyntax expression, string message)
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
	}
}
