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
		private readonly string context = "context";

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
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
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

			/*		public Task<PagedResults<Device>> GetPagedAsync(int page, int pageSize)
		{
			return MakeRequestAsync(() =>
			{
				return context.HttpClient.GetAsync<PagedResults<Device>>("Device", "GetPaged", page, pageSize);
			});
		}
*/
			var blocks = Block(
				ReturnStatement(
					InvocationExpression(
						IdentifierName("MakeRequestAsync")
					).AddArgumentListArguments(
						Argument(
							ParenthesizedLambdaExpression(
								Block(
									ReturnStatement(
										InvocationExpression(
											context.MemberAccess("HttpClient")
											.MemberAccess(
												GenericName("GetAsync")
												.AddTypeArgumentListArguments(
													GenericName("PagedResults")
													.AddTypeArgumentListArguments(
														ParseTypeName(t.Name)
													)
												)
											)
										).AddArgumentListArguments(
											t.Name.ToArgument(),
											"GetPaged".ToArgument(),
											Argument(IdentifierName("page")),
											Argument(IdentifierName("pageSize"))
										)
									)
								)
							)
						)
					)
				)
			);


			return method.WithBody(blocks);
		}
		#endregion
#region GetBy Id
		private MethodDeclarationSyntax generateGetById(string propertyName, Type t)
		{
			var pi = t.GetProperties();
			var id = "id";
			var method = MethodDeclaration(
				GenericName("Task").AddTypeArgumentListArguments(
					GenericName("Result").AddTypeArgumentListArguments(
						ParseTypeName(t.Name)
					)
				),
				"GetAsync")
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddParameterListParameters(
					Parameter(Identifier(id)).WithType(pi.GetKeyProperty().GetTypeSyntax())
				)
				.WithLeadingTrivia(
					ParseLeadingTrivia($@"/// <summary>
/// Gets the {t.Name} by its <paramref name=""id""/>.
/// </summary>
/// <param name=""id"">The identifier.</param>
/// <returns></returns>
")
				);

			var blocks = Block(
				ReturnStatement(
					InvocationExpression(
						IdentifierName("MakeRequestAsync")
					).AddArgumentListArguments(
						Argument(
							ParenthesizedLambdaExpression(
								Block(
									ReturnStatement(
										InvocationExpression(
											context.MemberAccess("HttpClient")
											.MemberAccess(
												GenericName("GetAsync")
												.AddTypeArgumentListArguments(
													GenericName("Result")
													.AddTypeArgumentListArguments(
														ParseTypeName(t.Name)
													)
												)
											)
										).AddArgumentListArguments(
											t.Name.ToArgument(),
											Argument(IdentifierName(id))
										)
									)
								)
							)
						)
					)
				)
			);

			return method.WithBody(blocks);
		}
#endregion
#region Post
		private MethodDeclarationSyntax generatePost(string propertyName, Type t)
		{
			var pi = t.GetProperties();
			var parameterName = t.Name.ToLower();

			var method = MethodDeclaration(
				GenericName("Task").AddTypeArgumentListArguments(
					GenericName("Result").AddTypeArgumentListArguments(
						ParseTypeName(t.Name)
					)
				),
				"PostAsync")
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddParameterListParameters(
					Parameter(Identifier(parameterName)).WithType(ParseTypeName(t.Name))
				)
				.WithLeadingTrivia(
					ParseLeadingTrivia($@"/// <summary>
/// Posts the passed <paramref name=""{parameterName}""/> to the server.
/// </summary>
/// <param name=""{parameterName}"">The {parameterName}.</param>
/// <returns></returns>

")
				);

			var blocks = Block(
				ReturnStatement(
					InvocationExpression(
						IdentifierName("MakeRequestAsync")
					).AddArgumentListArguments(
						Argument(
							ParenthesizedLambdaExpression(
								Block(
									ReturnStatement(
										InvocationExpression(
											context.MemberAccess("HttpClient")
											.MemberAccess(
												GenericName("PostAsync")
												.AddTypeArgumentListArguments(
													GenericName("Result")
													.AddTypeArgumentListArguments(
														ParseTypeName(t.Name)
													)
												)
											)
										).AddArgumentListArguments(
											t.Name.ToArgument(),
											Argument(
												BinaryExpression(SyntaxKind.CoalesceExpression,
													IdentifierName(parameterName),
													ThrowExpression(
														ObjectCreationExpression(ParseTypeName("ArgumentNullException"))
														.AddArgumentListArguments(
															Argument(
																InvocationExpression(
																	IdentifierName("nameof")
																).AddArgumentListArguments(
																	Argument(IdentifierName(parameterName))
																)
															)
														)
													)
												)
											)
										)
									)
								)
							)
						)
					)
				)
			);

			return method.WithBody(blocks);
		}
#endregion
#region Put
		private MethodDeclarationSyntax generatePut(string propertyName, Type t)
		{
			var pi = t.GetProperties();
			var parameterName = t.Name.ToLower();

			var method = MethodDeclaration(
				GenericName("Task").AddTypeArgumentListArguments(
					GenericName("Result").AddTypeArgumentListArguments(
						ParseTypeName(t.Name)
					)
				),
				"PutAsync")
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddParameterListParameters(
					Parameter(Identifier(parameterName)).WithType(ParseTypeName(t.Name))
				)
				.WithLeadingTrivia(
					ParseLeadingTrivia($@"/// <summary>
/// Puts the passed <paramref name=""{parameterName}""/> to the server.
/// </summary>
/// <param name=""{parameterName}"">The {parameterName}.</param>
/// <returns></returns>

")
				);

			var blocks = Block(
				ReturnStatement(
					InvocationExpression(
						IdentifierName("MakeRequestAsync")
					).AddArgumentListArguments(
						Argument(
							ParenthesizedLambdaExpression(
								Block(
									ReturnStatement(
										InvocationExpression(
											context.MemberAccess("HttpClient")
											.MemberAccess(
												GenericName("PutAsync")
												.AddTypeArgumentListArguments(
													GenericName("Result")
													.AddTypeArgumentListArguments(
														ParseTypeName(t.Name)
													)
												)
											)
										).AddArgumentListArguments(
											t.Name.ToArgument(),
											Argument(
												BinaryExpression(SyntaxKind.CoalesceExpression,
													IdentifierName(parameterName),
													ThrowExpression(
														ObjectCreationExpression(ParseTypeName("ArgumentNullException"))
														.AddArgumentListArguments(
															Argument(
																InvocationExpression(
																	IdentifierName("nameof")
																).AddArgumentListArguments(
																	Argument(IdentifierName(parameterName))
																)
															)
														)
													)
												)
											)
										)
									)
								)
							)
						)
					)
				)
			);

			return method.WithBody(blocks);
		}
#endregion
#region Delete
		private MethodDeclarationSyntax generateDelete(string propertyName, Type t)
		{
			var pi = t.GetProperties();
			var parameterName = t.Name.ToLower();

			var method = MethodDeclaration(
				GenericName("Task").AddTypeArgumentListArguments(
					GenericName("Result").AddTypeArgumentListArguments(
						ParseTypeName(t.Name)
					)
				),
				"DeleteAsync")
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddParameterListParameters(
					Parameter(Identifier(parameterName)).WithType(ParseTypeName(t.Name))
				)
				.WithLeadingTrivia(
					ParseLeadingTrivia($@"/// <summary>
/// Deletes the passed <paramref name=""{parameterName}""/> to the server.
/// </summary>
/// <param name=""{parameterName}"">The {parameterName}.</param>
/// <returns></returns>

")
				);

			var blocks = Block(
				ReturnStatement(
					InvocationExpression(
						IdentifierName("MakeRequestAsync")
					).AddArgumentListArguments(
						Argument(
							ParenthesizedLambdaExpression(
								Block(
									ReturnStatement(
										InvocationExpression(
											context.MemberAccess("HttpClient")
											.MemberAccess(
												GenericName("DeleteAsync")
												.AddTypeArgumentListArguments(
													GenericName("Result")
													.AddTypeArgumentListArguments(
														ParseTypeName(t.Name)
													)
												)
											)
										).AddArgumentListArguments(
											t.Name.ToArgument(),
											Argument(
												ParenthesizedExpression(
													BinaryExpression(SyntaxKind.CoalesceExpression,
														IdentifierName(parameterName),
														ThrowExpression(
															ObjectCreationExpression(ParseTypeName("ArgumentNullException"))
															.AddArgumentListArguments(
																Argument(
																	InvocationExpression(
																		IdentifierName("nameof")
																	).AddArgumentListArguments(
																		Argument(IdentifierName(parameterName))
																	)
																)
															)
														)
													)
												).MemberAccess(pi.GetKeyProperty().Name)
											)
										)
									)
								)
							)
						)
					)
				)
			);

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
			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.GetWithId))
			{
				@class = @class.AddMembers(generateGetById(propertyName, t));
			}

			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Post))
			{
				@class = @class.AddMembers(generatePost(propertyName, t));
			}

			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Put))
			{
				@class = @class.AddMembers(generatePut(propertyName, t));
			}

			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Delete))
			{
				@class = @class.AddMembers(generateDelete(propertyName, t));
			}

			unit = unit.AddMembers(NamespaceDeclaration(IdentifierName("Sannel.House.ServerSDK.Context")).AddMembers(@class));
			return unit;
		}
	}
}
