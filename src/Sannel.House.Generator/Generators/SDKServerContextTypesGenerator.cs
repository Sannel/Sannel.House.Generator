/* Copyright 2017 Sannel Software, L.L.C.

   Licensed under the Apache License, Version 2.0 (the ""License"");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

	   http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an ""AS IS"" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/
using Sannel.House.Generator.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using Sannel.House.Web.Base;

namespace Sannel.House.Generator.Generators
{
	public class SDKServerContextTypesGenerator : GeneratorBase
	{
		private ClassDeclarationSyntax addConstructor(ClassDeclarationSyntax @class)
		{
			var cons = ConstructorDeclaration(@class.Identifier)
				.AddModifiers(Token(SyntaxKind.InternalKeyword))
				.AddParameterListParameters(
					Parameter(Identifier("context"))
					.WithType(ParseTypeName("ServerContext"))
				).WithInitializer(
					ConstructorInitializer(SyntaxKind.BaseConstructorInitializer)
					.AddArgumentListArguments(
						Argument(IdentifierName("context"))
					)
				);

			var blocks = Block();

			return @class.AddMembers(cons.WithBody(blocks));
		}

		#region GetPaged
		private MethodDeclarationSyntax generatePagedMethod(string propertyName, Type t)
		{
			var method = MethodDeclaration(
				GenericName("Task").AddTypeArgumentListArguments(
					GenericName("PagedResults").AddTypeArgumentListArguments(ParseTypeName(t.Name))), 
				"GetPagedAsync")
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.WithLeadingTrivia(
					ParseLeadingTrivia($@"/// <summary>
/// Gets the {propertyName} asynchronous. Starting at page 1
/// </summary>
/// <returns></returns>
")
				);

			var blocks = Block(ReturnStatement(InvocationExpression(IdentifierName("GetPagedAsync")).AddArgumentListArguments(Argument(1.ToLiteral()))));

			return method.WithBody(blocks);
		}

		private MethodDeclarationSyntax generatePagedMethodWithPage(string propertyName, Type t)
		{

			var method = MethodDeclaration(
				GenericName("Task").AddTypeArgumentListArguments(
					GenericName("PagedResults").AddTypeArgumentListArguments(ParseTypeName(t.Name))), 
				"GetPagedAsync")
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddParameterListParameters(
					Parameter(Identifier("page")).WithType(ParseTypeName("int"))
				)
				.WithLeadingTrivia(
					ParseLeadingTrivia($@"/// <summary>
/// Gets the {propertyName} asynchronous. starting at <paramref name=""page""/>
/// </summary>
/// <param name=""page"">The page.</param>
/// <returns></returns>
")
				);

			var blocks = Block(ReturnStatement(InvocationExpression(IdentifierName("GetPagedAsync"))
				.AddArgumentListArguments(
					Argument(IdentifierName("page")),
					Argument(25.ToLiteral())
				)));

			return method.WithBody(blocks);
		}

		private MethodDeclarationSyntax generatePagedMethodWithPageAndPageSize(string propertyName, Type t)
		{

			var method = MethodDeclaration(
				GenericName("Task").AddTypeArgumentListArguments(
					GenericName("PagedResults").AddTypeArgumentListArguments(ParseTypeName(t.Name))), 
				"GetPagedAsync")
				.AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AsyncKeyword))
				.AddParameterListParameters(
					Parameter(Identifier("page")).WithType(ParseTypeName("int")),
					Parameter(Identifier("pageSize")).WithType(ParseTypeName("int"))
				)
				.WithLeadingTrivia(
					ParseLeadingTrivia($@"/// <summary>
/// Gets the {propertyName} asynchronous. starting at <paramref name=""page""/> retrieving <paramref name=""pageSize""/>
/// </summary>
/// <param name=""page"">The page.</param>
/// <param name=""pageSize"">Size of the page.</param>
/// <returns></returns>
")
				);

			var loginResult = "loginResult";
			var blocks = Block(LocalDeclarationStatement(Extensions.VariableDeclaration(loginResult,
				EqualsValueClause(
					InvocationExpression(
						GenericName("VerifyLoggedIn")
						.AddTypeArgumentListArguments(
							GenericName("PagedResults").AddTypeArgumentListArguments(ParseTypeName(t.Name))
						)
					)
				))));
			blocks = blocks.AddStatements(
				IfStatement(
					PrefixUnaryExpression(
						SyntaxKind.LogicalNotExpression,
						loginResult.MemberAccess("Success")
					),
					Block(
						ReturnStatement(
							IdentifierName(loginResult)
						)
					)
				)
			);

			var results = "results";

			blocks = blocks.AddStatements(
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						results,
						EqualsValueClause(
							AwaitExpression(
								InvocationExpression(
									"context".MemberAccess(
										"HttpClient"
									).MemberAccess(
										GenericName(
											"GetAsync"
										).AddTypeArgumentListArguments(
											GenericName("PagedResults")
											.AddTypeArgumentListArguments(
												ParseTypeName(t.Name)
											)
										)
									)
								).AddArgumentListArguments(
									Argument(t.Name.ToLiteral()),
									Argument("GetPaged".ToLiteral()),
									Argument(IdentifierName("page")),
									Argument(IdentifierName("pageSize"))
								)
							)
						)
					)
				)
			);

			blocks = blocks.AddStatements(
				IfStatement(
					BinaryExpression(SyntaxKind.LogicalAndExpression,
						results.MemberAccess("Success"),
						BinaryExpression(SyntaxKind.NotEqualsExpression,
							results.MemberAccess("Data"),
							LiteralExpression(SyntaxKind.NullLiteralExpression)
						)
					),
					Block(
						ReturnStatement(
							results.MemberAccess("Data")
						)
					)
				).WithElse(
					ElseClause(
						Block(
							ReturnStatement(
								InvocationExpression(
									IdentifierName("CreateRequestError")
								).AddArgumentListArguments(
									Argument(IdentifierName(results))
								)
							)
						)
					)
				)
			);
			/*

			var pResults = await context.HttpClient.GetAsync<PagedResults<Device>>("device", "GetPaged", page, pageSize);

			if (pResults.Success && pResults.Data != null)
			{
				return pResults.Data;
			}
			else
			{
				return CreateRequestError(pResults);
			}
			
			 */

			return method.WithBody(blocks);
		}
#endregion

		protected override CompilationUnitSyntax internalGenerate(string propertyName, Type t)
		{
			var unit = CompilationUnit();

			unit = unit.AddUsings("System").WithLeadingTrivia(GetLicenseComment());
			unit = unit.AddUsings("System.Threading.Tasks",
									"Sannel.House.ServerSDK.Results",
									"Sannel.House.ServerSDK.Models");

			var @class = ClassDeclaration($"{t.Name}Context")
						.AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword))
						.AddBaseListTypes(SimpleBaseType(ParseTypeName("ContextBase")));

			@class = addConstructor(@class);


			var ti = t.GetTypeInfo();
			var ga = ti.GetCustomAttribute<GenerationAttribute>() ?? new GenerationAttribute();

			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Get))
			{
				@class = @class.AddMembers(generatePagedMethod(propertyName, t));
				@class = @class.AddMembers(generatePagedMethodWithPage(propertyName, t));
				@class = @class.AddMembers(generatePagedMethodWithPageAndPageSize(propertyName, t));
			}

			unit = unit.AddMembers(NamespaceDeclaration(IdentifierName("Sannel.House.ServerSDK.Context")).AddMembers(@class));
			return unit;
		}
	}
}
