using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Sannel.House.Generator.Interfaces
{
    public interface ITaskBuilder
    {
		MethodDeclarationSyntax AsyncMethod(TypeArgumentListSyntax list, String name, BlockSyntax body);
		ParenthesizedLambdaExpressionSyntax ParenthesizedLambdaExpression(BlockSyntax blocks);
    }
}
