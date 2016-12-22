using Sannel.House.Generator.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp;

namespace Sannel.House.Generator.Common
{
	public class TaskBuilder : ITaskBuilder
	{
		public MethodDeclarationSyntax AsyncMethod(TypeArgumentListSyntax list, string name, BlockSyntax body)
		{
			var amethod = ParenthesizedLambdaExpression(body)
				.WithAsyncKeyword(Token(SyntaxKind.AsyncKeyword));

			return MethodDeclaration(
				GenericName(Identifier("Task"))
				.WithTypeArgumentList(list),
				Identifier(name)
				).WithBody(body).AddModifiers(Token(SyntaxKind.AsyncKeyword));
		}

		public ParenthesizedLambdaExpressionSyntax ParenthesizedLambdaExpression(BlockSyntax blocks)
		{
			return SF.ParenthesizedLambdaExpression(
						Block(
							ReturnStatement(
										InvocationExpression(
											Extensions.MemberAccess(
												IdentifierName("Task"),
												IdentifierName("Run")
											)
										).AddArgumentListArguments(
											Argument(
												SF.ParenthesizedLambdaExpression(
													blocks
												)
											)
										)
							)
						)
				);
		}
	}
}
