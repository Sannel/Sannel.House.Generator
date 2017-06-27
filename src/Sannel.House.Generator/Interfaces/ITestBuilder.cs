using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sannel.House.Generator.Interfaces
{
    public interface ITestBuilder
    {
		string[] Namespaces
		{
			get;
		}

		AttributeSyntax GetClassAttribute();

		AttributeSyntax GetMethodAttribute();

		ExpressionSyntax Equal(ExpressionSyntax expected, ExpressionSyntax actual);
		ExpressionSyntax Equal(ExpressionSyntax expected, ExpressionSyntax actual, string message);
		ExpressionSyntax NotEqual(ExpressionSyntax expected, ExpressionSyntax actual);
		ExpressionSyntax NotEqual(ExpressionSyntax expected, ExpressionSyntax actual, string message);
		ExpressionSyntax Null(ExpressionSyntax expression);
		ExpressionSyntax Null(ExpressionSyntax expression, string message);
		ExpressionSyntax NotNull(ExpressionSyntax expression);
		ExpressionSyntax NotNull(ExpressionSyntax expression, string message);

		ExpressionSyntax True(ExpressionSyntax expression);
		ExpressionSyntax True(ExpressionSyntax expression, string message);
		ExpressionSyntax False(ExpressionSyntax expression);
		ExpressionSyntax False(ExpressionSyntax expression, string message);

		ExpressionSyntax ThrowsAsync(TypeSyntax type, ExpressionSyntax expression);
		ExpressionSyntax ThrowsAsync<T>(ExpressionSyntax expression);
    }
}
