using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sannel.House.Generator.Interfaces
{
    public interface ITestBuilder
    {
		String[] Namespaces
		{
			get;
		}

		AttributeSyntax GetClassAttribute();

		AttributeSyntax GetMethodAttribute();

		ExpressionSyntax AssertAreEqual(ExpressionSyntax expected, ExpressionSyntax actual);
		ExpressionSyntax AssertAreEqual(ExpressionSyntax expected, ExpressionSyntax actual, String message);
		ExpressionSyntax AssertAreNotEqual(ExpressionSyntax expected, ExpressionSyntax actual);
		ExpressionSyntax AssertAreNotEqual(ExpressionSyntax expected, ExpressionSyntax actual, String message);
		ExpressionSyntax AssertIsNull(ExpressionSyntax expression);
		ExpressionSyntax AssertIsNull(ExpressionSyntax expression, String message);
		ExpressionSyntax AssertIsNotNull(ExpressionSyntax expression);
		ExpressionSyntax AssertIsNotNull(ExpressionSyntax expression, String message);

		ExpressionSyntax AssertIsTrue(ExpressionSyntax expression);
		ExpressionSyntax AssertIsTrue(ExpressionSyntax expression, String message);
		ExpressionSyntax AssertIsFalse(ExpressionSyntax expression);
		ExpressionSyntax AssertIsFalse(ExpressionSyntax expression, String message);
    }
}
