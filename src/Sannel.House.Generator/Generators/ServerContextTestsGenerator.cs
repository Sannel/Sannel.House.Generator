/* Copyright 2016 Sannel Software, L.L.C.

   Licensed under the Apache License, Version 2.0 (the ""License"");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

	   http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an ""AS IS"" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sannel.House.Generator.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Sannel.House.Generator.Common;

namespace Sannel.House.Generator.Generators
{
	public class ServerContextTestsGenerator : ICombinedGenerator
	{
		private readonly SyntaxToken key = Identifier("key");
		private Dictionary<String, String> variables;
		private IHttpClientBuilder clientBuilder;
		private ITaskBuilder taskBuilder;
		private ITestBuilder testBuilder;

		public ServerContextTestsGenerator()
		{
		}

		private StatementSyntax assertStatment(ExpressionSyntax expected, ExpressionSyntax actual, String method = "AreEqual")
		{
			if(String.Compare(method, "AreNotEqual", true) == 0)
			{
				return ExpressionStatement(testBuilder.AssertAreNotEqual(expected, actual));
			}
			return ExpressionStatement(testBuilder.AssertAreEqual(expected, actual));
		}

		private StatementSyntax assertIsNull(ExpressionSyntax test, String message)
		{
			return ExpressionStatement(testBuilder.AssertIsNull(test, message));
		}

		private StatementSyntax assertIsNotNull(ExpressionSyntax test, String message)
		{
			return ExpressionStatement(testBuilder.AssertIsNotNull(test, message));
		}

		private StatementSyntax assertIsTrue(ExpressionSyntax test, string v)
		{
			return ExpressionStatement(testBuilder.AssertIsTrue(test, v));
		}

		private ArgumentSyntax generateRunTaskWrapper(BlockSyntax blocks, params ParameterSyntax[] parameters)
		{
			return Argument(taskBuilder
					.ParenthesizedLambdaExpression(blocks)
					.AddParameterListParameters(parameters)
				);
		}

		private StatementSyntax[] getStandardTests(Type t, SyntaxToken resultsVariable, String status, ExpressionSyntax defaultKey)
		{
			var items = new List<StatementSyntax>();
			items.Add(
				assertStatment(
					Extensions.MemberAccess(
						IdentifierName("RequestStatus"),
						IdentifierName(status)
					)
					, Extensions.MemberAccess(
						IdentifierName(resultsVariable),
						IdentifierName("Status")
					)
				)
			);
			items.Add(
				assertIsNull(
					Extensions.MemberAccess(
						IdentifierName(resultsVariable),
						IdentifierName("Data")
					),
					"Data should not be null"
				)
			);

			items.Add(
				assertStatment(
					defaultKey,
					Extensions.MemberAccess(
						IdentifierName(resultsVariable),
						IdentifierName("Key")
					)
				)
			);

			return items.ToArray();
		}

		private StatementSyntax[] getStandardTests(Type t, SyntaxToken resultsVariable, String status, SyntaxToken keyVariableName)
		{
			var items = new List<StatementSyntax>();
			items.Add(
				assertStatment(
					Extensions.MemberAccess(
						IdentifierName("RequestStatus"),
						IdentifierName(status)
					)
					, Extensions.MemberAccess(
						IdentifierName(resultsVariable),
						IdentifierName("Status")
					)
				)
			);
			items.Add(
				assertIsNull(
					Extensions.MemberAccess(
						IdentifierName(resultsVariable),
						IdentifierName("Data")
					),
					"Data should be null"
				)
			);

			items.Add(
				assertStatment(
					IdentifierName(keyVariableName),
					Extensions.MemberAccess(
						IdentifierName(resultsVariable),
						IdentifierName("Key")
					)
				)
			);

			return items.ToArray();
		}

		private AttributeSyntax getTestAttribute()
		{
			return testBuilder.GetMethodAttribute();
		}

		public MemberDeclarationSyntax createGetMethod(Type t, PropertyInfo[] pi)
		{
			var method = MethodDeclaration(ParseTypeName("Task"), $"Get{t.Name}AsyncTest")
				.AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.AsyncKeyword))
				.AddAttributeLists(
					AttributeList()
					.AddAttributes(
						getTestAttribute()
					)
				);

			foreach (var p in pi)
			{
				if (!p.ShouldIgnore())
				{
					var ts = p.GetTypeSyntax();
					method = method.AddBodyStatements(
						LocalDeclarationStatement(
						Extensions.VariableDeclaration($"_{p.Name}",
							EqualsValueClause(Extensions.GetDefaultValue(ts)),
							ts.ToString()
						)));
				}
			}


			var stub = Identifier("stub");

			method = method.AddBodyStatements(
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(stub.Text,
						EqualsValueClause(
							ObjectCreationExpression(ParseTypeName($"StubI{t.Name}"))
							.AddArgumentListArguments()
						)
					)).WithLeadingTrivia(Comment("// Setup Stub"))
				);

			foreach (var p in pi)
			{
				if (!p.ShouldIgnore())
				{
					method = method.AddBodyStatements(
						ExpressionStatement(
						InvocationExpression(
							MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
								IdentifierName(stub),
								IdentifierName($"{p.Name}_Get")))
							.AddArgumentListArguments(
								Argument(ParenthesizedLambdaExpression(IdentifierName($"_{p.Name}")))
							)
						),
						ExpressionStatement(
							InvocationExpression(
								MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
									IdentifierName(stub),
									IdentifierName($"{p.Name}_Set"))
							).AddArgumentListArguments(
								Argument(
									ParenthesizedLambdaExpression(
										AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
											IdentifierName($"_{p.Name}"),
											IdentifierName("v"))
									)
									.AddParameterListParameters(
										Parameter(Identifier("v"))
									)
								)
							)
						)
					);
				}
			}

			var create = Identifier("create");

			method = method.AddBodyStatements(
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(create.Text,
						EqualsValueClause(
							ObjectCreationExpression(ParseTypeName("StubICreateHelper"))
							.AddArgumentListArguments()
						)
					)
				).WithLeadingTrivia(Comment("// create helper")),
				ExpressionStatement(
					InvocationExpression(
						MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
							IdentifierName(create),
							IdentifierName($"Create{t.Name}"))
					)
					.AddArgumentListArguments(
						Argument(
							ParenthesizedLambdaExpression(
								IdentifierName(stub)
							)
						)
					)
				)
			);

			var settings = Identifier("settings");

			method = method.AddBodyStatements(
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(settings.Text,
						EqualsValueClause(
							ObjectCreationExpression(ParseTypeName("StubIServerSettings"))
							.AddArgumentListArguments()
						)
					)
				).WithLeadingTrivia(Comment("// settings ")),
				ExpressionStatement(
					InvocationExpression(
						MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
							IdentifierName(settings),
							IdentifierName("ServerUri_Get"))
					)
					.AddArgumentListArguments(
						Argument(
							ParenthesizedLambdaExpression(
								LiteralExpression(SyntaxKind.NullLiteralExpression)
							)
						)
					)
				)
			);

			var httpClient = Identifier("httpClient");

			method = method.AddBodyStatements(
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(httpClient.Text,
						EqualsValueClause(
							ObjectCreationExpression(ParseTypeName("StubIHttpClient"))
							.AddArgumentListArguments()
						)
					)
				).WithLeadingTrivia(Comment("// Http Client"))
			);

			var k = pi.GetKeyProperty();
			var keySy = ParseTypeName(k.PropertyType.Name);

			Random rand = new Random();

			var serverContext = Identifier("serverContext");
			var results = Identifier("results");
			method = method.AddBodyStatements(
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(serverContext.Text,
						EqualsValueClause(
							ObjectCreationExpression(ParseTypeName("ServerContext"))
							.AddArgumentListArguments(
								Argument(IdentifierName(settings)),
								Argument(IdentifierName(create)),
								Argument(IdentifierName(httpClient))
							)
						)
					)
				).WithLeadingTrivia(Comment("// Server ")),
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(key.Text,
						EqualsValueClause(keySy.GetRandomValue(rand))
						, keySy.GetTypeString()
					)
				),
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						results.Text,
						EqualsValueClause(
							AwaitExpression(
								InvocationExpression(
									MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										IdentifierName(serverContext),
										IdentifierName($"Get{t.Name}Async")
									)
								)
								.AddArgumentListArguments(
									Argument(IdentifierName(key))
								)
							)
						)
					)
				).WithLeadingTrivia(Comment("// leading"))
			);

			method = method.AddBodyStatements(
				getStandardTests(t, results, "ServerUriNotSet", keySy.GetDefaultValue())
			);

			method = method.AddBodyStatements(
				ExpressionStatement(
					InvocationExpression(
						Extensions.MemberAccess(
							IdentifierName(settings),
							IdentifierName("ServerUri_Get")
						)
					)
					.AddArgumentListArguments(
						Argument(
							ParenthesizedLambdaExpression(
								ObjectCreationExpression(ParseTypeName("Uri"))
								.AddArgumentListArguments(
									Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("http://test")))
								)
							)
						)
					)
				).WithLeadingTrivia(Comment("// login exception")),
				ExpressionStatement(
					InvocationExpression(
						Extensions.MemberAccess(
							IdentifierName(httpClient),
							IdentifierName("PostAsync")
						)
					).AddArgumentListArguments(
						generateRunTaskWrapper(Block(
							ReturnStatement(
								ObjectCreationExpression(ParseTypeName("HttpClientResult"))
								.WithInitializer(
									InitializerExpression(SyntaxKind.ObjectInitializerExpression)
									.AddExpressions(
										AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
											IdentifierName("StatusCode"),
											Extensions.MemberAccess(
												IdentifierName("HttpStatusCode"),
												IdentifierName("Ok")
											)
										),
										AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
											IdentifierName("Content"),
											LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(""))
										)
									)
								)
						)),
							Parameter(Identifier("uri")),
							Parameter(Identifier("d"))
						)
					)
				),
				ExpressionStatement(
					InvocationExpression(
						Extensions.MemberAccess(
							IdentifierName(httpClient),
							IdentifierName("GetCookieValue")
						)
					).AddArgumentListArguments(
						Argument(
							ParenthesizedLambdaExpression(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("Value")))
							.AddParameterListParameters(
								Parameter(Identifier("u")),
								Parameter(Identifier("name"))
							)
						)
					)
				),
				ExpressionStatement(
					AwaitExpression(
						InvocationExpression(
							Extensions.MemberAccess(
								IdentifierName(serverContext),
								IdentifierName("LoginAsync")
							)
						).AddArgumentListArguments(
							Argument("user".ToLiteral()),
							Argument("pass".ToLiteral())
						)
					)
				),
				ExpressionStatement(
					InvocationExpression(
						Extensions.MemberAccess(
							IdentifierName(settings),
							IdentifierName("ServerUri_Get")
						)
					).AddArgumentListArguments(
						Argument(
							ParenthesizedLambdaExpression(
								ObjectCreationExpression(ParseTypeName("Uri"))
								.AddArgumentListArguments(
									Argument(
										"/cheese".ToLiteral()
									),
									Argument(
										Extensions.MemberAccess(
											IdentifierName("UriKind"),
											IdentifierName("Relative")
										)
									)
								)
							)
						)
					)
				),
				ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						IdentifierName(results),
						AwaitExpression(
							InvocationExpression(
								Extensions.MemberAccess(
									IdentifierName(serverContext),
									IdentifierName($"Get{t.Name}Async")
								)
							).AddArgumentListArguments(
								Argument(IdentifierName(key))
							)
						)
					).WithLeadingTrivia(Comment("// invalid uri"))
				)
			);

			method = method.AddBodyStatements(
				getStandardTests(t, results, "ServerUriIsNotValid", keySy.GetDefaultValue())
			);

			var methodHit = Identifier("methodHit");
			var expectedException = Identifier("expectedException");
			var uri = Identifier("uri");

			method = method.AddBodyStatements(
				ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						IdentifierName(key),
						keySy.GetRandomValue(rand)
					)
				).WithLeadingTrivia(Comment("// Unable to connect to server")),
				ExpressionStatement(
					InvocationExpression(
						Extensions.MemberAccess(
							IdentifierName(settings),
							IdentifierName("ServerUri_Get")
						)
					).AddArgumentListArguments(
						Argument(
							ParenthesizedLambdaExpression(
								ObjectCreationExpression(
									ParseTypeName("Uri")
								).AddArgumentListArguments(
									Argument(
										"http://test".ToLiteral()
									)
								)
							)
						)
					)
				),
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(expectedException.Text,
						EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression)),
						"Exception"
					)
				),
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(methodHit.Text,
						EqualsValueClause(LiteralExpression(SyntaxKind.FalseLiteralExpression)),
						"bool"
					)
				),
				ExpressionStatement(
					InvocationExpression(
						Extensions.MemberAccess(
							IdentifierName(httpClient),
							IdentifierName("GetAsync")
						)
					).AddArgumentListArguments(
						generateRunTaskWrapper(
							Block(
								ExpressionStatement(
									AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
										IdentifierName(methodHit),
										LiteralExpression(SyntaxKind.TrueLiteralExpression)
									)
								),
								assertStatment(
									ObjectCreationExpression(
										IdentifierName("Uri")
									).AddArgumentListArguments(
										Argument(
											//$"http://test/api/{t.Name}/{{key}}".ToLiteral()
											$"http://test/api/{t.Name}/".ToInterpolatedString(key.ToStringToken())
										)
									),
									IdentifierName(uri)
								),
								ExpressionStatement(
									AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
										IdentifierName(expectedException),
										ObjectCreationExpression(ParseTypeName("COMException"))
										.AddArgumentListArguments(
											Argument("Message1".ToLiteral()),
											Argument((-2147012867).ToLiteral())
										)
									)
								),
								ThrowStatement(IdentifierName(expectedException)),
								ReturnStatement(
									ObjectCreationExpression(ParseTypeName("HttpClientResult"))
									.AddArgumentListArguments()
								)
							),
							Parameter(uri)
						)
					)
				),
				ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						IdentifierName(results),
						AwaitExpression(
							InvocationExpression(
								Extensions.MemberAccess(
									IdentifierName(serverContext),
									IdentifierName($"Get{t.Name}Async")
								)
							).AddArgumentListArguments(
								Argument(IdentifierName(key))
							)
						)
					)
				),
				assertIsTrue(IdentifierName(methodHit), "Method not called")
			);

			method = method.AddBodyStatements(
				getStandardTests(t, results, "UnableToConnectToServer", IdentifierName(key)));

			method = method.AddBodyStatements(
				assertStatment(IdentifierName(expectedException),
					Extensions.MemberAccess(IdentifierName(results),
						IdentifierName("Exception"))
			));

			if (String.Compare(variables.GetValue("IsUWP"), "1", true) == 0)
			{
				method = method.AddBodyStatements(
					ExpressionStatement(
						AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
							IdentifierName(methodHit),
							LiteralExpression(SyntaxKind.FalseLiteralExpression)
						)
					).WithLeadingTrivia(Comment("// COMException ")),
					ExpressionStatement(
						InvocationExpression(
							Extensions.MemberAccess(
								IdentifierName(httpClient),
								IdentifierName("GetAsync")
							)
						).AddArgumentListArguments(
							generateRunTaskWrapper(
								Block(
									ExpressionStatement(
										AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
											IdentifierName(methodHit),
											LiteralExpression(SyntaxKind.TrueLiteralExpression)
										)
									),
									ExpressionStatement(
										AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
											IdentifierName(expectedException),
											ObjectCreationExpression(ParseTypeName("COMException"))
											.AddArgumentListArguments(
												Argument("Message2".ToLiteral())
											)
										)
									),
									ThrowStatement(
										IdentifierName(expectedException)
									),
									ReturnStatement(
										ObjectCreationExpression(ParseTypeName("HttpClientResult"))
										.AddArgumentListArguments()
									)
								),
								Parameter(uri)
							)
						)
					),
					ExpressionStatement(
						AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
							IdentifierName(results),
							AwaitExpression(
								InvocationExpression(
									Extensions.MemberAccess(
										IdentifierName(serverContext),
										IdentifierName($"Get{t.Name}Async")
									)
								).AddArgumentListArguments(
									Argument(IdentifierName(key))
								)
							)
						)
					),
					assertIsTrue(IdentifierName(methodHit), "Method not called")
				);
				method = method.AddBodyStatements(
					getStandardTests(t, results, "Exception", key)
				);
				method = method.AddBodyStatements(
					assertStatment(IdentifierName(expectedException),
						Extensions.MemberAccess(
							IdentifierName(results),
							IdentifierName("Exception")
						)
					)
				);
			}

			method = method.AddBodyStatements(
				ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						IdentifierName(methodHit),
						LiteralExpression(SyntaxKind.FalseLiteralExpression)
					)
				).WithLeadingTrivia(Comment("// Exception ")),
				ExpressionStatement(
					InvocationExpression(
						Extensions.MemberAccess(
							IdentifierName(httpClient),
							IdentifierName("GetAsync")
						)
					).AddArgumentListArguments(
						generateRunTaskWrapper(
							Block(
								ExpressionStatement(
									AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
										IdentifierName(methodHit),
										LiteralExpression(SyntaxKind.TrueLiteralExpression)
									)
								),
								ExpressionStatement(
									AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
										IdentifierName(expectedException),
										ObjectCreationExpression(ParseTypeName("Exception"))
										.AddArgumentListArguments(
											Argument("Message3".ToLiteral())
										)
									)
								),
								ThrowStatement(
									IdentifierName(expectedException)
								),
								ReturnStatement(
									ObjectCreationExpression(ParseTypeName("HttpClientResult"))
									.AddArgumentListArguments()
								)
							),
							Parameter(uri)
						)
					)
				),
				ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						IdentifierName(results),
						AwaitExpression(
							InvocationExpression(
								Extensions.MemberAccess(
									IdentifierName(serverContext),
									IdentifierName($"Get{t.Name}Async")
								)
							).AddArgumentListArguments(
								Argument(IdentifierName(key))
							)
						)
					)
				),
				assertIsTrue(IdentifierName(methodHit), "Method not called")
			);
			method = method.AddBodyStatements(
				getStandardTests(t, results, "Exception", key)
			);
			method = method.AddBodyStatements(
				assertStatment(IdentifierName(expectedException),
					Extensions.MemberAccess(
						IdentifierName(results),
						IdentifierName("Exception")
					)
				)
			);
			method = method.AddBodyStatements(
				ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						IdentifierName(methodHit),
						LiteralExpression(SyntaxKind.FalseLiteralExpression)
					)
				).WithLeadingTrivia(Comment("// Unauthorized ")),
				ExpressionStatement(
					InvocationExpression(
						Extensions.MemberAccess(
							IdentifierName(httpClient),
							IdentifierName("GetAsync")
						)
					).AddArgumentListArguments(
						generateRunTaskWrapper(
							Block(
								ExpressionStatement(
									AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
										IdentifierName(methodHit),
										LiteralExpression(SyntaxKind.TrueLiteralExpression)
									)
								),
								ReturnStatement(
									ObjectCreationExpression(ParseTypeName("HttpClientResult"))
									.AddArgumentListArguments()
									.WithInitializer(
										InitializerExpression(SyntaxKind.ObjectInitializerExpression)
										.AddExpressions(
											AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
												IdentifierName("StatusCode"),
												Extensions.MemberAccess(
													IdentifierName("HttpStatusCode"),
													IdentifierName("Unauthorized")
												)
											),
											AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
												IdentifierName("Content"),
												"{Test: 'cheese'".ToLiteral()
											)
										)
									)
								)
							),
							Parameter(uri)
						)
					)
				),
				ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						IdentifierName(results),
						AwaitExpression(
							InvocationExpression(
								Extensions.MemberAccess(
									IdentifierName(serverContext),
									IdentifierName($"Get{t.Name}Async")
								)
							).AddArgumentListArguments(
								Argument(IdentifierName(key))
							)
						)
					)
				),
				assertIsTrue(IdentifierName(methodHit), "Method not called")
			);
			method = method.AddBodyStatements(
				getStandardTests(t, results, "Error", key)
			);
			method = method.AddBodyStatements(
				ExpressionStatement(
					InvocationExpression(
						Extensions.MemberAccess(
							IdentifierName("Assert"),
							IdentifierName("IsInstanceOfType")
						)
					)
					.AddArgumentListArguments(
						Argument(
							Extensions.MemberAccess(
								IdentifierName(results),
								IdentifierName("Exception")
							)
						),
						Argument(
							TypeOfExpression(
								ParseTypeName("JsonReaderException")
							)
						)
					)
				)
			);

			var exp = Identifier("exp");
			var expected = Identifier("expected");
			method = method.AddBodyStatements(
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						exp.Text,
						EqualsValueClause(
							ObjectCreationExpression(ParseTypeName($"StubI{t.Name}"))
							.AddArgumentListArguments()
						)
					)
				).WithLeadingTrivia(Comment("// Success Test"))
			);

			var jobj = Identifier("jobj");
			var blocks = Block();

			blocks = blocks.AddStatements(
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						jobj.Text,
						EqualsValueClause(
							ObjectCreationExpression(ParseTypeName("JObject"))
							.AddArgumentListArguments()
						)
					)
				)
			);


			foreach (var prop in pi)
			{
				if (!prop.ShouldIgnore())
				{
					method = method.AddBodyStatements(
						ExpressionStatement(
							InvocationExpression(
								Extensions.MemberAccess(
									IdentifierName(exp),
									IdentifierName($"{prop.Name}_Get")
								)
							)
							.AddArgumentListArguments(
								Argument(
									ParenthesizedLambdaExpression(
										(prop.IsKey()) ? IdentifierName(key) : prop.GetTypeSyntax().GetRandomValue(rand)
									)
								)
							)
						)
					);
					//jobj.Add(new JProperty(nameof(expected.Id), expected.Id));
					blocks = blocks.AddStatements(
						ExpressionStatement(
							InvocationExpression(
								Extensions.MemberAccess(
									IdentifierName(jobj),
									IdentifierName("Add")
								)
							)
							.AddArgumentListArguments(
								Argument(
									ObjectCreationExpression(
										ParseTypeName("JProperty")
									).AddArgumentListArguments(
										Argument(
											InvocationExpression(IdentifierName("nameof"))
											.AddArgumentListArguments(
												Argument(
													Extensions.MemberAccess(
														IdentifierName(expected),
														IdentifierName(prop.Name)
													)
												)
											)
										),
										Argument(
											Extensions.MemberAccess(
												IdentifierName(expected),
												IdentifierName(prop.Name)
											)
										)
									)
								)
							)
						)
					);
				}
			}

			method = method.AddBodyStatements(
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						expected.Text,
						EqualsValueClause(
							IdentifierName(exp)
						),
						$"I{t.Name}"
					)
				)
			);

			method = method.AddBodyStatements(blocks.Statements.ToArray());

			method = method.AddBodyStatements(
				ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						IdentifierName(methodHit),
						false.ToLiteral()
					)
				),
				ExpressionStatement(
					InvocationExpression(
						Extensions.MemberAccess(
							IdentifierName(httpClient),
							IdentifierName("GetAsync")
						)
					)
					.AddArgumentListArguments(
						generateRunTaskWrapper(
							Block().AddStatements(
								ExpressionStatement(
									AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
										IdentifierName(methodHit),
										true.ToLiteral()
									)
								),
								ReturnStatement(
									ObjectCreationExpression(
										ParseTypeName("HttpClientResult")
									)
									.WithInitializer(
										InitializerExpression(SyntaxKind.ObjectInitializerExpression)
										.AddExpressions(
											AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
												IdentifierName("StatusCode"),
												Extensions.MemberAccess(
													IdentifierName("HttpStatusCode"),
													IdentifierName("Ok")
												)
											),
											AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
												IdentifierName("Content"),
												InvocationExpression(
													Extensions.MemberAccess(
														IdentifierName(jobj),
														IdentifierName("ToString")
													)
												).AddArgumentListArguments()
											)
										)
									)
								)
							),
							Parameter(Identifier("uri"))
						)
					)
				)
			);

			var actual = Identifier("actual");

			method = method.AddBodyStatements(
				ExpressionStatement(
					AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						IdentifierName(results),
						AwaitExpression(
							InvocationExpression(
								Extensions.MemberAccess(
									IdentifierName(serverContext),
									IdentifierName($"Get{t.Name}Async")
								)
							)
							.AddArgumentListArguments(
								Argument(IdentifierName(key))
							)
						)
					)
				),
				assertIsTrue(IdentifierName(methodHit), "Method not called"),
				assertStatment(
					Extensions.MemberAccess(
						IdentifierName("RequestStatus"),
						IdentifierName("Success")
					),
					Extensions.MemberAccess(
						IdentifierName(results),
						IdentifierName("Status")
					)
				),
				assertStatment(
					IdentifierName(key),
					Extensions.MemberAccess(
						IdentifierName(results),
						IdentifierName("Key")
					)
				),
				assertIsNull(
					Extensions.MemberAccess(
						IdentifierName(results),
						IdentifierName("Exception")
					),
					"Exception should be null"
				),
				assertIsNotNull(
					Extensions.MemberAccess(
						IdentifierName(results),
						IdentifierName("Data")
					),
					"Data should not be null"
				),
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						actual.Text,
						EqualsValueClause(
							Extensions.MemberAccess(
								IdentifierName(results),
								IdentifierName("Data")
							)
						)
					)
				)
			);

			foreach(var prop in pi)
			{
				if (!prop.ShouldIgnore())
				{
					method = method.AddBodyStatements(
						assertStatment(
							Extensions.MemberAccess(
								IdentifierName(expected),
								IdentifierName(prop.Name)
							),
							Extensions.MemberAccess(
								IdentifierName(actual),
								IdentifierName(prop.Name)
							)
						)
					);
				}
			}

			/*
			 */

			return method;
		}


		private ClassDeclarationSyntax addType(PropertyWithName p, ClassDeclarationSyntax @class)
		{
			var tn = ParseTypeName(p.PropertyName);
			var pi = p.Type.GetProperties();
			var method = createGetMethod(p.Type, pi)
				.WithLeadingTrivia(Trivia(RegionDirectiveTrivia(true)
					.WithEndOfDirectiveToken(
						Token(TriviaList().Add(PreprocessingMessage(p.PropertyName)),
							SyntaxKind.EndOfDirectiveToken,
							TriviaList())
					)
				));

			method = method.WithTrailingTrivia(
				Trivia(
					EndRegionDirectiveTrivia(true)
				)
			);

			return @class.AddMembers(method);
		}

		public void Generate(IList<PropertyWithName> props, string baseSaveDirectory, RunConfig config)
		{
			var dir = Path.Combine(baseSaveDirectory, config.Directory);
			if(!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			clientBuilder = config.HttpBuilder;
			taskBuilder = config.TaskBuilder;
			testBuilder = config.TestBuilder;
			variables = config.Variables;

			var cu = CompilationUnit();
			cu = cu.AddUsing("System").WithLeadingTrivia(GeneratorBase.GetLicenseComment());
			cu = cu.AddUsings(new string[]
			{
				"System.Threading.Tasks",
				"Newtonsoft.Json",
				"Newtonsoft.Json.Linq"
			});
			cu = cu.AddUsings(clientBuilder.Namespace);
			cu = cu.AddUsings(testBuilder.Namespaces);

			if(String.Compare(variables.GetValue("IsUWP"), "1", true) == 0)
			{
				cu = cu.AddUsing("System.Runtime.InteropServices");
			}

			var namesp = NamespaceDeclaration(IdentifierName("Sannel.House.ServerSDK.Tests"));

			var @class = ClassDeclaration("ServerContextTests")
				.AddModifiers(Token(SyntaxKind.PublicKeyword),
				Token(SyntaxKind.PartialKeyword));


			foreach(var prop in props)
			{
				@class = addType(prop, @class);
			}

			namesp = namesp.AddMembers(@class);

			cu = cu.AddMembers(namesp).NormalizeWhitespace("\t", true);
			using(var writer = new StreamWriter(File.OpenWrite(Path.Combine(dir, config.FileName))))
			{
				cu.WriteTo(writer);
			}
		}
	}
}
