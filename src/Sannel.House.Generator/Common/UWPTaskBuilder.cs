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
	public class UWPTaskBuilder : ITaskBuilder
	{
		public MethodDeclarationSyntax AsyncMethod(TypeArgumentListSyntax list, string name, BlockSyntax body)
		{
			var amethod = SF.ParenthesizedLambdaExpression(body)
				.WithAsyncKeyword(Token(SyntaxKind.AsyncKeyword));

			return MethodDeclaration(
				GenericName(Identifier("IAsyncOperation"))
				.WithTypeArgumentList(list),
				Identifier(name)
				).AddBodyStatements(
				ReturnStatement().WithExpression(
					InvocationExpression(
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						InvocationExpression(MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
							IdentifierName("Task"),
							IdentifierName("Run")
						)).WithArgumentList(
							ArgumentList().AddArguments(Argument(amethod))
						),
						IdentifierName("AsAsyncOperation")
					)
				)));
		}

		public ParenthesizedLambdaExpressionSyntax ParenthesizedLambdaExpression(BlockSyntax blocks)
		{
			return SF.ParenthesizedLambdaExpression(
						Block(
							ReturnStatement(
								InvocationExpression(
									Extensions.MemberAccess(
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
										),
										IdentifierName("AsAsyncOperation")
									)
							).AddArgumentListArguments()
						)
					)
				);
		}
	}
}
