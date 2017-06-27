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
using System.Threading.Tasks;

namespace Sannel.House.Generator.Generators
{
	public class SDKServerContextTypesTestsGenerator : GeneratorBase
	{
		private MethodDeclarationSyntax generateGetPagedTest(string propertyName, Type t)
		{
			var method = MethodDeclaration(ParseTypeName("Task"), "GetPagedTests")
							.AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AsyncKeyword))
							.AddAttributeLists(AttributeList().AddAttributes(TestBuilder.GetMethodAttribute()));

			var mClient = "mClient";
			var sc = "sc";
			var result = "result";

			var blocks = Block(
					LocalDeclarationStatement(
						Extensions.VariableDeclaration(result,
							EqualsValueClause(
								AwaitExpression(
									InvocationExpression(
										sc.MemberAccess(propertyName, "GetPagedAsync")
									).AddArgumentListArguments(
										Argument(1.ToLiteral()),
										Argument(20.ToLiteral())
									)
								)
							)
						)
					),
					ExpressionStatement(TestBuilder.False(result.MemberAccess("Success"))),
					ExpressionStatement(TestBuilder.NotNull(result.MemberAccess("Errors"))),
					ExpressionStatement(TestBuilder.Equal(1.ToLiteral(), result.MemberAccess("Errors", "Count"))),
					ExpressionStatement(TestBuilder.Equal(
						"Errors".MemberAccess("ServerContext_PleaseLogin"),
						ElementAccessExpression(result.MemberAccess("Errors"))
						.AddArgumentListArguments(
							Argument(0.ToLiteral())
						)
						))
				);

			blocks = blocks.AddStatements(
				ExpressionStatement(AwaitExpression(InvocationExpression(sc.MemberAccess("FakeLoginAsync"))
					.AddArgumentListArguments(
						Argument(IdentifierName(mClient))
					)
				)));

			var controller = "controller";
			var segments = "segments";

			blocks = blocks.AddStatements(
				ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						mClient.MemberAccess("Get"),
						ParenthesizedLambdaExpression(
							Block(
								ExpressionStatement(
									TestBuilder.Equal(3.ToLiteral(), segments.MemberAccess("Length"))
								),
								ExpressionStatement(
									TestBuilder.Equal(t.Name.ToLiteral(), IdentifierName(controller))
								),
								ExpressionStatement(
									TestBuilder.Equal("GetPaged".ToLiteral(), segments.ElementAccess(0))
								),
								ExpressionStatement(
									TestBuilder.Equal(1.ToLiteral(), segments.ElementAccess(1))
								),
								ExpressionStatement(
									TestBuilder.Equal(20.ToLiteral(), segments.ElementAccess(2))
								),
								ReturnStatement(
									InvocationExpression(
										MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
											ObjectCreationExpression(
												GenericName("ClientResult")
													.AddTypeArgumentListArguments(
														GenericName("PagedResults")
															.AddTypeArgumentListArguments(
																ParseTypeName(t.Name)
															)
													)
											).AddArgumentListArguments()
											.WithInitializer(
												InitializerExpression(SyntaxKind.ObjectInitializerExpression,
													SingletonSeparatedList<ExpressionSyntax>(
														AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
															IdentifierName("Success"),
															false.ToLiteral()
														)
													)
												)
											),
											IdentifierName("AddError")
										)
									).AddArgumentListArguments(
										Argument("Request Error".ToLiteral())
									)
								)
							)
						).AddParameterListParameters(
							Parameter(Identifier(controller)),
							Parameter(Identifier(segments))
						)
					)
				)
				);

			blocks = blocks.AddStatements(
				ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						IdentifierName(result),
						AwaitExpression(
							InvocationExpression(
								sc.MemberAccess(propertyName, "GetPagedAsync")
							).AddArgumentListArguments(
								Argument(1.ToLiteral()),
								Argument(20.ToLiteral())
							)
						)
					)
				),
				ExpressionStatement(
					TestBuilder.False(result.MemberAccess("Success"))
				),
				TestBuilder.NotNull(result.MemberAccess("Errors")).ToStatement(),
				TestBuilder.Equal(2.ToLiteral(), result.MemberAccess("Errors", "Count")).ToStatement(),
				TestBuilder.Equal("Request Error".ToLiteral(), result.MemberAccess("Errors").ElementAccess(0)).ToStatement(),
				TestBuilder.Equal("Errors".MemberAccess("ServerContext_ErrorMakingRequest"), result.MemberAccess("Errors").ElementAccess(1)).ToStatement()
			);

			blocks = blocks.AddStatements(
				AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					mClient.MemberAccess("Get"),
					ParenthesizedLambdaExpression(
						Block(
							ExpressionStatement(
								TestBuilder.Equal(3.ToLiteral(), segments.MemberAccess("Length"))
							),
							ExpressionStatement(
								TestBuilder.Equal(t.Name.ToLiteral(), IdentifierName(controller))
							),
							ExpressionStatement(
								TestBuilder.Equal("GetPaged".ToLiteral(), segments.ElementAccess(0))
							),
							ExpressionStatement(
								TestBuilder.Equal(1.ToLiteral(), segments.ElementAccess(1))
							),
							ExpressionStatement(
								TestBuilder.Equal(2.ToLiteral(), segments.ElementAccess(2))
							),
							ReturnStatement(
								ObjectCreationExpression(
									GenericName("ClientResult")
									.AddTypeArgumentListArguments(
										GenericName("PagedResults")
										.AddTypeArgumentListArguments(
											ParseTypeName(t.Name)
										)
									)
								).AddArgumentListArguments()
								.WithInitializer(
									InitializerExpression(SyntaxKind.ObjectInitializerExpression,
										SingletonSeparatedList<ExpressionSyntax>(
											AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
												IdentifierName("Data"),
												InvocationExpression(
													ObjectCreationExpression(
														GenericName("PagedResults")
														.AddTypeArgumentListArguments(
															ParseTypeName(t.Name)
														)
													).AddArgumentListArguments()
													.WithInitializer(
														InitializerExpression(SyntaxKind.ObjectInitializerExpression,
															SingletonSeparatedList<ExpressionSyntax>(
																AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
																	IdentifierName("Success"),
																	false.ToLiteral()
																)
															)
														)
													).MemberAccess(
														"AddError"
													)
												).AddArgumentListArguments(
													Argument("Cannot connect to database".ToLiteral())
												)
											)
										).Add(
											AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
												IdentifierName("Success"),
												true.ToLiteral()
											)
										)
									)
								)
							)
						)
					).AddParameterListParameters(
						Parameter(Identifier(controller)),
						Parameter(Identifier(segments))
					)
				).ToStatement()
			);

			blocks = blocks.AddStatements(
				AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					IdentifierName(result),
					AwaitExpression(
						InvocationExpression(
							sc.MemberAccess(propertyName, "GetPagedAsync")
						).AddArgumentListArguments(
							1.ToArgument(),
							2.ToArgument()
						)
					)
				).ToStatement(),
				TestBuilder.False(result.MemberAccess("Success")).ToStatement(),
				TestBuilder.NotNull(result.MemberAccess("Errors")).ToStatement(),
				TestBuilder.Equal(1.ToLiteral(), result.MemberAccess("Errors", "Count")).ToStatement(),
				TestBuilder.Equal("Cannot connect to database".ToLiteral(), result.MemberAccess("Errors").ElementAccess(0)).ToStatement()
			);

			var var1 = "var1";
			var var2 = "var2";

			var rand = new Random();
			blocks = blocks.AddStatements(
				t.GenerateRandomObject(var1, rand)
			);

			blocks = blocks.AddStatements(
				t.GenerateRandomObject(var2, rand)
			);

			blocks = blocks.AddStatements(
				AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					mClient.MemberAccess("Get"),
					ParenthesizedLambdaExpression(
						Block(
							ExpressionStatement(
								TestBuilder.Equal(3.ToLiteral(), segments.MemberAccess("Length"))
							),
							ExpressionStatement(
								TestBuilder.Equal(t.Name.ToLiteral(), IdentifierName(controller))
							),
							ExpressionStatement(
								TestBuilder.Equal("GetPaged".ToLiteral(), segments.ElementAccess(0))
							),
							ExpressionStatement(
								TestBuilder.Equal(1.ToLiteral(), segments.ElementAccess(1))
							),
							ExpressionStatement(
								TestBuilder.Equal(20.ToLiteral(), segments.ElementAccess(2))
							),
							ReturnStatement(
								ObjectCreationExpression(
									GenericName("ClientResult")
									.AddTypeArgumentListArguments(
										GenericName("PagedResults")
										.AddTypeArgumentListArguments(
											ParseTypeName(t.Name)
										)
									)
								).AddArgumentListArguments()
								.WithInitializer(
									InitializerExpression(SyntaxKind.ObjectInitializerExpression,
										SingletonSeparatedList<ExpressionSyntax>(
											AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
												IdentifierName("Data"),
													ObjectCreationExpression(
														GenericName("PagedResults")
														.AddTypeArgumentListArguments(
															ParseTypeName(t.Name)
														)
													).AddArgumentListArguments()
													.WithInitializer(
														InitializerExpression(SyntaxKind.ObjectInitializerExpression,
															SingletonSeparatedList<ExpressionSyntax>(
																AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
																	IdentifierName("Success"),
																	true.ToLiteral()
																)
															).Add(
																AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
																	IdentifierName("Data"),
																	ObjectCreationExpression(GenericName("List").AddTypeArgumentListArguments(ParseTypeName(t.Name)))
																	.AddArgumentListArguments()
																	.WithInitializer(
																		InitializerExpression(SyntaxKind.CollectionInitializerExpression,
																			SeparatedList<ExpressionSyntax>()
																			.Add(IdentifierName(var1))
																			.Add(IdentifierName(var2))
																		)
																	)
																)
															).Add(
																AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
																	IdentifierName("CurrentPage"),
																	1.ToLiteral()
																)
															).Add(
																AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
																	IdentifierName("PageSize"),
																	2.ToLiteral()
																)
															).Add(
																AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
																	IdentifierName("TotalResults"),
																	20.ToLiteral()
																)
															)
														)
												)
											)
										).Add(
											AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
												IdentifierName("Success"),
												true.ToLiteral()
											)
										)
									)
								)
							)
						)
					).AddParameterListParameters(
						Parameter(Identifier(controller)),
						Parameter(Identifier(segments))
					)
				).ToStatement()
			);

			var list = "list";
			var actual = "actual";

			blocks = blocks.AddStatements(
				AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					IdentifierName(result),
					AwaitExpression(
						InvocationExpression(
							sc.MemberAccess(propertyName, "GetPagedAsync")
						).AddArgumentListArguments(
							1.ToArgument(),
							20.ToArgument()
						)
					)
				).ToStatement(),
				TestBuilder.True(result.MemberAccess("Success")).ToStatement(),
				TestBuilder.NotNull(result.MemberAccess("Data")).ToStatement(),
				TestBuilder.Equal(1.ToLiteral(), result.MemberAccess("CurrentPage")).ToStatement(),
				TestBuilder.Equal(2.ToLiteral(), result.MemberAccess("PageSize")).ToStatement(),
				TestBuilder.Equal(20.ToLiteral(), result.MemberAccess("TotalResults")).ToStatement(),
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						list,
						EqualsValueClause(
							InvocationExpression(
								result.MemberAccess("Data", "ToList")
							).AddArgumentListArguments()
						)
					)
				),
				TestBuilder.Equal(2.ToLiteral(), list.MemberAccess("Count")).ToStatement(),
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						actual,
						EqualsValueClause(
							list.ElementAccess(0)
						)
					)
				),
				TestBuilder.NotNull(IdentifierName(actual)).ToStatement()
			);

			var pi = t.GetProperties();
			foreach(var p in pi)
			{
				if (!p.ShouldIgnore())
				{
					blocks = blocks.AddStatements(
						TestBuilder.Equal(var1.MemberAccess(p.Name), actual.MemberAccess(p.Name)).ToStatement()
					);
				}
			}
			blocks = blocks.AddStatements(
				AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					IdentifierName(actual),
					list.ElementAccess(1)
				).ToStatement()
			);
			foreach(var p in pi)
			{
				if (!p.ShouldIgnore())
				{
					blocks = blocks.AddStatements(
						TestBuilder.Equal(var2.MemberAccess(p.Name), actual.MemberAccess(p.Name)).ToStatement()
					);
				}
			}

			method = method.WithBody(Block(
				UsingStatement(
					Block(
					UsingStatement(
						blocks
					).WithDeclaration(
						Extensions.VariableDeclaration(sc,
							EqualsValueClause(ObjectCreationExpression(ParseTypeName("ServerContext")).AddArgumentListArguments(Argument(IdentifierName(mClient)))
						)
					)))
				).WithDeclaration(
					Extensions.VariableDeclaration(mClient,
						EqualsValueClause(ObjectCreationExpression(ParseTypeName("MockHttpClient")).AddArgumentListArguments())
					)
				)
				));

			return method;
		}

		private MethodDeclarationSyntax generateGetByIdTest(string propertyName, Type t)
		{
			var method = MethodDeclaration(ParseTypeName("Task"), "GetTests")
							.AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AsyncKeyword))
							.AddAttributeLists(AttributeList().AddAttributes(TestBuilder.GetMethodAttribute()));
			var rand = new Random();

			var mClient = "mClient";
			var sc = "sc";
			var result = "result";
			var pi = t.GetProperties();
			var key = pi.GetKeyProperty();
			var keyst = key.GetTypeSyntax();
			var keyId = "keyid";
			var keyvalue = IdentifierName(keyId);

			var blocks = Block(
					LocalDeclarationStatement(
						Extensions.VariableDeclaration(result,
							EqualsValueClause(
								AwaitExpression(
									InvocationExpression(
										sc.MemberAccess(propertyName, "GetAsync")
									).AddArgumentListArguments(
										Argument(keyst.GetDefaultValue())
									)
								)
							)
						)
					),
					LocalDeclarationStatement(
						Extensions.VariableDeclaration(keyId,
							EqualsValueClause(
								CastExpression(keyst, keyst.GetRandomValue(rand)) // ensure this is the correct type
							)
						)
					),
					ExpressionStatement(TestBuilder.False(result.MemberAccess("Success"))),
					ExpressionStatement(TestBuilder.NotNull(result.MemberAccess("Errors"))),
					ExpressionStatement(TestBuilder.Equal(1.ToLiteral(), result.MemberAccess("Errors", "Count"))),
					ExpressionStatement(TestBuilder.Equal(
						"Errors".MemberAccess("ServerContext_PleaseLogin"),
						ElementAccessExpression(result.MemberAccess("Errors"))
						.AddArgumentListArguments(
							0.ToArgument()
						)
						))
				);

			blocks = blocks.AddStatements(
				ExpressionStatement(AwaitExpression(InvocationExpression(sc.MemberAccess("FakeLoginAsync"))
					.AddArgumentListArguments(
						Argument(IdentifierName(mClient))
					)
				)));

			var controller = "controller";
			var segments = "segments";

			blocks = blocks.AddStatements(
				ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						mClient.MemberAccess("Get"),
						ParenthesizedLambdaExpression(
							Block(
								ExpressionStatement(
									TestBuilder.Equal(1.ToLiteral(), segments.MemberAccess("Length"))
								),
								ExpressionStatement(
									TestBuilder.Equal(t.Name.ToLiteral(), IdentifierName(controller))
								),
								ExpressionStatement(
									TestBuilder.Equal(keyvalue, segments.ElementAccess(0))
								),
								ReturnStatement(
									InvocationExpression(
										MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
											ObjectCreationExpression(
												GenericName("ClientResult")
													.AddTypeArgumentListArguments(
														GenericName("Result")
															.AddTypeArgumentListArguments(
																ParseTypeName(t.Name)
															)
													)
											).AddArgumentListArguments()
											.WithInitializer(
												InitializerExpression(SyntaxKind.ObjectInitializerExpression,
													SingletonSeparatedList<ExpressionSyntax>(
														AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
															IdentifierName("Success"),
															false.ToLiteral()
														)
													)
												)
											),
											IdentifierName("AddError")
										)
									).AddArgumentListArguments(
										Argument("Request Error".ToLiteral())
									)
								)
							)
						).AddParameterListParameters(
							Parameter(Identifier(controller)),
							Parameter(Identifier(segments))
						)
					)
				)
				);

			blocks = blocks.AddStatements(
				ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						IdentifierName(result),
						AwaitExpression(
							InvocationExpression(
								sc.MemberAccess(propertyName, "GetAsync")
							).AddArgumentListArguments(
								Argument(keyvalue)
							)
						)
					)
				),
				ExpressionStatement(
					TestBuilder.False(result.MemberAccess("Success"))
				),
				TestBuilder.NotNull(result.MemberAccess("Errors")).ToStatement(),
				TestBuilder.Equal(2.ToLiteral(), result.MemberAccess("Errors", "Count")).ToStatement(),
				TestBuilder.Equal("Request Error".ToLiteral(), result.MemberAccess("Errors").ElementAccess(0)).ToStatement(),
				TestBuilder.Equal("Errors".MemberAccess("ServerContext_ErrorMakingRequest"), result.MemberAccess("Errors").ElementAccess(1)).ToStatement()
			);

			blocks = blocks.AddStatements(
				AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					mClient.MemberAccess("Get"),
					ParenthesizedLambdaExpression(
						Block(
							ExpressionStatement(
								TestBuilder.Equal(1.ToLiteral(), segments.MemberAccess("Length"))
							),
							ExpressionStatement(
								TestBuilder.Equal(t.Name.ToLiteral(), IdentifierName(controller))
							),
							ExpressionStatement(
								TestBuilder.Equal(keyvalue, segments.ElementAccess(0))
							),
							ReturnStatement(
								ObjectCreationExpression(
									GenericName("ClientResult")
									.AddTypeArgumentListArguments(
										GenericName("Result")
										.AddTypeArgumentListArguments(
											ParseTypeName(t.Name)
										)
									)
								).AddArgumentListArguments()
								.WithInitializer(
									InitializerExpression(SyntaxKind.ObjectInitializerExpression,
										SingletonSeparatedList<ExpressionSyntax>(
											AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
												IdentifierName("Data"),
												InvocationExpression(
													ObjectCreationExpression(
														GenericName("Result")
														.AddTypeArgumentListArguments(
															ParseTypeName(t.Name)
														)
													).AddArgumentListArguments()
													.WithInitializer(
														InitializerExpression(SyntaxKind.ObjectInitializerExpression,
															SingletonSeparatedList<ExpressionSyntax>(
																AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
																	IdentifierName("Success"),
																	false.ToLiteral()
																)
															)
														)
													).MemberAccess(
														"AddError"
													)
												).AddArgumentListArguments(
													Argument("Cannot connect to database".ToLiteral())
												)
											)
										).Add(
											AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
												IdentifierName("Success"),
												true.ToLiteral()
											)
										)
									)
								)
							)
						)
					).AddParameterListParameters(
						Parameter(Identifier(controller)),
						Parameter(Identifier(segments))
					)
				).ToStatement()
			);

			blocks = blocks.AddStatements(
				AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					IdentifierName(result),
					AwaitExpression(
						InvocationExpression(
							sc.MemberAccess(propertyName, "GetAsync")
						).AddArgumentListArguments(
							Argument(keyvalue)
						)
					)
				).ToStatement(),
				TestBuilder.False(result.MemberAccess("Success")).ToStatement(),
				TestBuilder.NotNull(result.MemberAccess("Errors")).ToStatement(),
				TestBuilder.Equal(1.ToLiteral(), result.MemberAccess("Errors", "Count")).ToStatement(),
				TestBuilder.Equal("Cannot connect to database".ToLiteral(), result.MemberAccess("Errors").ElementAccess(0)).ToStatement()
			);

			var var1 = "var1";

			blocks = blocks.AddStatements(
				t.GenerateRandomObject(var1, rand, keyvalue)
			);

			blocks = blocks.AddStatements(
				AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					mClient.MemberAccess("Get"),
					ParenthesizedLambdaExpression(
						Block(
							ExpressionStatement(
								TestBuilder.Equal(1.ToLiteral(), segments.MemberAccess("Length"))
							),
							ExpressionStatement(
								TestBuilder.Equal(t.Name.ToLiteral(), IdentifierName(controller))
							),
							ExpressionStatement(
								TestBuilder.Equal(keyvalue, segments.ElementAccess(0))
							),
							ReturnStatement(
								ObjectCreationExpression(
									GenericName("ClientResult")
									.AddTypeArgumentListArguments(
										GenericName("Result")
										.AddTypeArgumentListArguments(
											ParseTypeName(t.Name)
										)
									)
								).AddArgumentListArguments()
								.WithInitializer(
									InitializerExpression(SyntaxKind.ObjectInitializerExpression,
										SingletonSeparatedList<ExpressionSyntax>(
											AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
												IdentifierName("Data"),
													ObjectCreationExpression(
														GenericName("Result")
														.AddTypeArgumentListArguments(
															ParseTypeName(t.Name)
														)
													).AddArgumentListArguments()
													.WithInitializer(
														InitializerExpression(SyntaxKind.ObjectInitializerExpression,
															SingletonSeparatedList<ExpressionSyntax>(
																AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
																	IdentifierName("Success"),
																	true.ToLiteral()
																)
															).Add(
																AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
																	IdentifierName("Data"),
																	IdentifierName(var1)
																)
															)
														)
												)
											)
										).Add(
											AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
												IdentifierName("Success"),
												true.ToLiteral()
											)
										)
									)
								)
							)
						)
					).AddParameterListParameters(
						Parameter(Identifier(controller)),
						Parameter(Identifier(segments))
					)
				).ToStatement()
			);

			var actual = "actual";

			blocks = blocks.AddStatements(
				AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					IdentifierName(result),
					AwaitExpression(
						InvocationExpression(
							sc.MemberAccess(propertyName, "GetAsync")
						).AddArgumentListArguments(
							Argument(keyvalue)
						)
					)
				).ToStatement(),
				TestBuilder.True(result.MemberAccess("Success")).ToStatement(),
				TestBuilder.NotNull(result.MemberAccess("Data")).ToStatement(),
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						actual,
						EqualsValueClause(
							result.MemberAccess("Data")
						)
					)
				),
				TestBuilder.NotNull(IdentifierName(actual)).ToStatement()
			);

			foreach(var p in pi)
			{
				if (!p.ShouldIgnore())
				{
					blocks = blocks.AddStatements(
						TestBuilder.Equal(var1.MemberAccess(p.Name), actual.MemberAccess(p.Name)).ToStatement()
					);
				}
			}

			method = method.WithBody(Block(
				UsingStatement(
					Block(
					UsingStatement(
						blocks
					).WithDeclaration(
						Extensions.VariableDeclaration(sc,
							EqualsValueClause(ObjectCreationExpression(ParseTypeName("ServerContext")).AddArgumentListArguments(Argument(IdentifierName(mClient)))
						)
					)))
				).WithDeclaration(
					Extensions.VariableDeclaration(mClient,
						EqualsValueClause(ObjectCreationExpression(ParseTypeName("MockHttpClient")).AddArgumentListArguments())
					)
				)
				));

			return method;
		}


		private MethodDeclarationSyntax generatePostTest(string propertyName, Type t)
		{
			var method = MethodDeclaration(ParseTypeName("Task"), "PostTests")
							.AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AsyncKeyword))
							.AddAttributeLists(AttributeList().AddAttributes(TestBuilder.GetMethodAttribute()));

			var mClient = "mClient";
			var sc = "sc";
			var result = "result";
			var pi = t.GetProperties();
			var key = pi.GetKeyProperty();
			var keyst = key.GetTypeSyntax();
			var keyId = "keyid";
			var keyvalue = IdentifierName(keyId);
			var var1 = "var1";

			var blocks = Block(
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(var1,
						EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression)), 
						t.Name
					)
				),
				LocalDeclarationStatement(
					VariableDeclaration(GenericName("Result").AddTypeArgumentListArguments(ParseTypeName(t.Name)))
					.WithVariables(new SeparatedSyntaxList<VariableDeclaratorSyntax>()
					.Add(						
						VariableDeclarator(result)
						.WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression)))
					))
				)
			);

			blocks = blocks.AddStatements(
				AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					IdentifierName(result),
					AwaitExpression(
						InvocationExpression(
							sc.MemberAccess(propertyName, "PostAsync")
						).AddArgumentListArguments(
							Argument(IdentifierName(var1))
						)
					)
				).ToStatement(),
				TestBuilder.False(result.MemberAccess("Success")).ToStatement(),
				TestBuilder.NotNull(result.MemberAccess("Errors", "Count")).ToStatement(),
				TestBuilder.Equal("Errors".MemberAccess("ServerContext_PleaseLogin"), result.MemberAccess("Errors").ElementAccess(0)).ToStatement(),
				AwaitExpression(
					InvocationExpression(
						sc.MemberAccess("FakeLoginAsync")
					).AddArgumentListArguments(
						Argument(IdentifierName(mClient))
					)
				).ToStatement()
			);


			blocks = blocks.AddStatements(
				AwaitExpression(
					TestBuilder.ThrowsAsync<ArgumentNullException>(
						ParenthesizedLambdaExpression(
							InvocationExpression(sc.MemberAccess(propertyName, "PostAsync"))
							.AddArgumentListArguments(
								Argument(IdentifierName(var1))
							)
						)
					)
				).ToStatement()
			);

			/*					
								await Assert.ThrowsAsync<ArgumentNullException>(async () =>
					{
						result = await sc.Devices.PostAsync(device);
					});

*/

			method = method.WithBody(Block(
				UsingStatement(
					Block(
					UsingStatement(
						blocks
					).WithDeclaration(
						Extensions.VariableDeclaration(sc,
							EqualsValueClause(ObjectCreationExpression(ParseTypeName("ServerContext")).AddArgumentListArguments(Argument(IdentifierName(mClient)))
						)
					)))
				).WithDeclaration(
					Extensions.VariableDeclaration(mClient,
						EqualsValueClause(ObjectCreationExpression(ParseTypeName("MockHttpClient")).AddArgumentListArguments())
					)
				)
				));

			return method;
		}

		protected override CompilationUnitSyntax internalGenerate(string propertyName, Type t)
		{
			var filename = $"{t.Name}ContextTests";
			var unit = CompilationUnit();

			unit = unit.AddUsing("System").WithLeadingTrivia(GetLicenseComment());
			unit = unit.AddUsings(" Sannel.House.ServerSDK.Models",
				"Sannel.House.ServerSDK.Results",
				"Sannel.House.ServerSDK.Tests.Mocks",
				"System.Collections.Generic",
				"System.Linq",
				"System.Threading.Tasks");
			unit = unit.AddUsings(TestBuilder.Namespaces);

			var @class = ClassDeclaration(filename)
				.AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword));

			var att = TestBuilder.GetClassAttribute();
			if (att != null)
			{
				@class = @class.AddAttributeLists(
					AttributeList().AddAttributes(att)
				);
			}
			var ti = t.GetTypeInfo();
			var ga = ti.GetCustomAttribute<GenerationAttribute>() ?? new GenerationAttribute();

			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Get))
			{
				@class = @class.AddMembers(generateGetPagedTest(propertyName, t));
			}
			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.GetWithId))
			{
				@class = @class.AddMembers(generateGetByIdTest(propertyName, t));
			}
			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Post))
			{
				@class = @class.AddMembers(generatePostTest(propertyName, t));
				/*@class = @class.AddMembers(generatePostTest(controllerName, propertyName, t));
				@class = @class.AddMembers(SF.MethodDeclaration(SF.ParseTypeName("void"),
					"postPreCall")
					.AddModifiers(SF.Token(SyntaxKind.PartialKeyword))
					.AddParameterListParameters(
						SF.Parameter(SF.Identifier("data")).WithType(SF.ParseTypeName(t.Name)),
						SF.Parameter(SF.Identifier("wrapper")).WithType(SF.ParseTypeName("ContextWrapper"))
					).WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken))
					.WithLeadingTrivia(SF.Comment("// used to make sure reference tables have data needed for a test to succeed"))
				);*/
		}
			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Put))
			{
				/*@class = @class.AddMembers(generatePutTest(controllerName, propertyName, t));
				@class = @class.AddMembers(SF.MethodDeclaration(SF.ParseTypeName("void"),
					"putPreCall")
					.AddModifiers(SF.Token(SyntaxKind.PartialKeyword))
					.AddParameterListParameters(
						SF.Parameter(SF.Identifier("data")).WithType(SF.ParseTypeName(t.Name)),
						SF.Parameter(SF.Identifier("wrapper")).WithType(SF.ParseTypeName("ContextWrapper"))
					).WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken))
					.WithLeadingTrivia(SF.Comment("// used to make sure reference tables have data needed for a test to succeed"))
				);*/
			}

			return unit.AddMembers(NamespaceDeclaration(IdentifierName("Sannel.House.ServerSDK.Tests.Context")).AddMembers(@class));
		}
	}
}
