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
				return new String[]{ "XUnit"};
			}
		}

		public ExpressionSyntax AssertAreEqual(ExpressionSyntax expected, ExpressionSyntax actual)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "Equal")
				).AddArgumentListArguments(
					Argument(expected),
					Argument(actual)
				);
		}

		public ExpressionSyntax AssertAreEqual(ExpressionSyntax expected, ExpressionSyntax actual, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "Equal")
				).AddArgumentListArguments(
					Argument(expected),
					Argument(actual)
				);
		}

		public ExpressionSyntax AssertAreNotEqual(ExpressionSyntax expected, ExpressionSyntax actual)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "NotEqual")
				).AddArgumentListArguments(
					Argument(expected),
					Argument(actual)
				);
		}

		public ExpressionSyntax AssertAreNotEqual(ExpressionSyntax expected, ExpressionSyntax actual, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "NotEqual")
				).AddArgumentListArguments(
					Argument(expected),
					Argument(actual)
				);
		}

		public ExpressionSyntax AssertIsFalse(ExpressionSyntax expression)
		{
			throw new NotImplementedException();
		}

		public ExpressionSyntax AssertIsFalse(ExpressionSyntax expression, string message)
		{
			throw new NotImplementedException();
		}

		public ExpressionSyntax AssertIsNotNull(ExpressionSyntax expression)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "NotNull")
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax AssertIsNotNull(ExpressionSyntax expression, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "NotNull")
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax AssertIsNull(ExpressionSyntax expression)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "Null")
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax AssertIsNull(ExpressionSyntax expression, string message)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "Null")
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax AssertIsTrue(ExpressionSyntax expression)
		{
			return InvocationExpression(
					Extensions.MemberAccess("Assert", "True")
				).AddArgumentListArguments(
					Argument(expression)
				);
		}

		public ExpressionSyntax AssertIsTrue(ExpressionSyntax expression, string message)
		{
			return AssertIsTrue(expression);
		}

		public AttributeSyntax GetClassAttribute()
		{
			return null;
		}

		public AttributeSyntax GetMethodAttribute()
		{
			return Attribute(IdentifierName("Fact"));
		}
	}
}
