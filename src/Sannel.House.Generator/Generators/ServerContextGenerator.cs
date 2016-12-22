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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using System.Reflection;
using Sannel.House.Generator.Interfaces;
using Sannel.House.Generator.Common;

namespace Sannel.House.Generator.Generators
{
	public class ServerContextGenerator : ICombinedGenerator
	{
		private SyntaxToken keyName = Identifier("key");
		private SyntaxToken helperName = Identifier("helper");
		private IHttpClientBuilder httpBuilder;
		private ITaskBuilder taskBuilder;
		private Dictionary<String, String> variables { get; set; }

		private StatementSyntax[] createCommonResultIfStatments(Type t, PropertyInfo[] pi, String resultName)
		{
			var key = pi.GetKeyProperty();
			var list = new List<StatementSyntax>();
			/*			var check = checkCommon();
				if(check.Item1 != RequestStatus.Success)
				{
					return new TemperatureEntryResult(check.Item1, null, default(Guid));
				}
				var builder = check.Item2;
*/

			var check = Identifier("check");
			list.Add(
				LocalDeclarationStatement(
					Extensions.VariableDeclaration(check.Text,
						EqualsValueClause(
							InvocationExpression(
								IdentifierName("checkCommon")
							).AddArgumentListArguments()
						)
					)
				)
			);

			list.Add(
				IfStatement(
					BinaryExpression(SyntaxKind.NotEqualsExpression,
						Extensions.MemberAccess(
							IdentifierName(check),
							IdentifierName("Item1")
						),
						Extensions.MemberAccess(
							IdentifierName("RequestStatus"),
							IdentifierName("Success")
						)
					),
					Block(
						ReturnStatement(
							ObjectCreationExpression(ParseTypeName(resultName))
							.AddArgumentListArguments(
								Argument(Extensions.MemberAccess(
									IdentifierName(check),
									IdentifierName("Item1")
								)),
								Argument(LiteralExpression(SyntaxKind.NullLiteralExpression)),
								Argument(IdentifierName(keyName))
							)
						)
					)
				)
			);

			list.Add(
				LocalDeclarationStatement(
					Extensions.VariableDeclaration("builder",
						EqualsValueClause(
							Extensions.MemberAccess(
								IdentifierName(check),
								IdentifierName("Item2")
							)
						)
					)
				)
			);
			


			/*
				HttpBaseProtocolFilter httpFilter = new HttpBaseProtocolFilter();
				httpFilter.CookieManager.SetCookie(cookie);*/

			return list.ToArray();
		}

		private StatementSyntax getPropertyValue(SyntaxToken obj, SyntaxToken jObject, String propertyName, Type propertyType)
		{
			String typeName = propertyType.Name;
			if (propertyType.GenericTypeArguments != null && propertyType.GenericTypeArguments.Length > 0)
			{
				if (propertyType.Name.StartsWith("Nullable"))
				{
					typeName = $"{propertyType.GenericTypeArguments[0]}?";
				}
			}
			return ExpressionStatement(AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName(obj),
						IdentifierName(propertyName)
					),
					InvocationExpression(
						MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
							IdentifierName(jObject),
							GenericName("GetPropertyValue")
							.AddTypeArgumentListArguments(
								ParseTypeName(typeName)
							)
						)
					).AddArgumentListArguments(
						Argument(
							InvocationExpression(
								IdentifierName("nameof")
							).AddArgumentListArguments(
								Argument(
									MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										IdentifierName(obj),
										IdentifierName(propertyName)
									)
								)
							)
						)
					)
				));
		}

		private StatementSyntax[] createGetCall(Type t, PropertyInfo[] pi)
		{
			var result = Identifier("result");
			var list = new List<StatementSyntax>();
			var key = pi.GetKeyProperty();
			list.Add(ExpressionStatement(
				AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName("builder"),
						IdentifierName("Path")
					),
					InterpolatedStringExpression(Token(SyntaxKind.InterpolatedStringStartToken))
						.AddContents(
							InterpolatedStringText()
							.WithTextToken(
								Token(TriviaList(),
									SyntaxKind.InterpolatedStringTextToken,
									$"/api/{t.Name}/",
									$"/api/{t.Name}/",
									TriviaList())),
							Interpolation(IdentifierName(keyName)))
						)
					)
				);
			list.Add(LocalDeclarationStatement(
					VariableDeclaration(ParseTypeName("HttpClientResult"))
						.AddVariables(
							VariableDeclarator(result)
								.WithInitializer(
									EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression))
								)
						)
				));

			var ce = Identifier("ce");

			var tryStatment = TryStatement()
				.WithBlock(
					Block(
					ExpressionStatement(
						AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
							IdentifierName(result),
							AwaitExpression(
								InvocationExpression(
									MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										IdentifierName("client"),
										IdentifierName("GetAsync")
									)
								)
								.AddArgumentListArguments(
									Argument(
										MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
											IdentifierName("builder"),
											IdentifierName("Uri")
										)
									)
								)
							)
					)
				)));
			if(String.Compare(variables.GetValue("IsUWP"), "1") == 0)
			{
				tryStatment = tryStatment.AddCatches(
					CatchClause()
					.WithDeclaration(
						CatchDeclaration(IdentifierName("COMException"))
						.WithIdentifier(ce)
					)
					.AddBlockStatements(
						IfStatement(
							BinaryExpression(SyntaxKind.EqualsExpression,
								MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
									IdentifierName(ce),
									IdentifierName("HResult")
								),
								LiteralExpression(SyntaxKind.NumericLiteralExpression,
									Literal(-2147012867)
								)
							),
							Block()
							.AddStatements(
								ReturnStatement(
									ObjectCreationExpression(ParseTypeName($"{t.Name}Result"))
									.AddArgumentListArguments(
										Argument(
											MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
												IdentifierName(ServerSDKStatusConstants.EnumName),
												IdentifierName(ServerSDKStatusConstants.UnableToConnectToServer))
										),
										Argument(
											LiteralExpression(SyntaxKind.NullLiteralExpression)
										)
										,
										Argument(
											IdentifierName(keyName)
										),
										Argument(
											IdentifierName(ce)
										)
									)
								)
							)
						),
						ReturnStatement(
							ObjectCreationExpression(ParseTypeName($"{t.Name}Result"))
							.AddArgumentListArguments(
								Argument(
									MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										IdentifierName(ServerSDKStatusConstants.EnumName),
										IdentifierName(ServerSDKStatusConstants.Exception))
								),
								Argument(
									LiteralExpression(SyntaxKind.NullLiteralExpression)
								)
								,
								Argument(
									IdentifierName(keyName)
								),
								Argument(
									IdentifierName(ce)
								)
							)
						)
					));
			}
			tryStatment = tryStatment.AddCatches(
				CatchClause()
					.WithDeclaration(
						CatchDeclaration(IdentifierName("Exception"))
						.WithIdentifier(ce)
					)
					.AddBlockStatements(
						ReturnStatement(
							ObjectCreationExpression(ParseTypeName($"{t.Name}Result"))
							.AddArgumentListArguments(
								Argument(
									MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										IdentifierName(ServerSDKStatusConstants.EnumName),
										IdentifierName(ServerSDKStatusConstants.Exception))
								),
								Argument(
									LiteralExpression(SyntaxKind.NullLiteralExpression)
								)
								,
								Argument(
									IdentifierName(keyName)
								),
								Argument(
									IdentifierName(ce)
								)
							)
						)
					)
				).WithTrailingTrivia(Whitespace(Environment.NewLine + Environment.NewLine));

			list.Add(tryStatment);

			var res = Identifier("res");
			var token = Identifier("token");
			var item = Identifier("item");

			var setStatements = Block();
			foreach (var prop in pi)
			{
				if (!prop.ShouldIgnore())
				{
					setStatements = setStatements.AddStatements(getPropertyValue(item, token, prop.Name, prop.PropertyType));
				}
			}

			var block = Block()
				.AddStatements(
					LocalDeclarationStatement(
						VariableDeclaration(IdentifierName("var"))
						.AddVariables(
							VariableDeclarator(res)
							.WithInitializer(
								EqualsValueClause(
									MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										IdentifierName(result),
										IdentifierName("Content")
									)
								)
							)
						)
					).WithTrailingTrivia(Whitespace(Environment.NewLine + Environment.NewLine)),
					TryStatement()
					.AddBlockStatements(
						LocalDeclarationStatement(
							VariableDeclaration(IdentifierName("var"))
							.AddVariables(
								VariableDeclarator(token)
								.WithInitializer(
									EqualsValueClause(
										InvocationExpression(
											MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
												IdentifierName("JObject"),
												IdentifierName("Parse")
											)
										).AddArgumentListArguments(
											Argument(IdentifierName(res))
										)
									)
								)
							)
						),
						LocalDeclarationStatement(
							VariableDeclaration(IdentifierName("var"))
							.AddVariables(
								VariableDeclarator(item)
								.WithInitializer(
									EqualsValueClause(
										InvocationExpression(
											MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
												IdentifierName(helperName),
												IdentifierName($"Create{t.Name}")
											)
										).AddArgumentListArguments()
									)
								)
							)
						),
						IfStatement(
							BinaryExpression(SyntaxKind.EqualsExpression,
								MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
									IdentifierName(token),
									IdentifierName("Type")
								),
								MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
									IdentifierName("JTokenType"),
									IdentifierName("Object")
								)
							),
							setStatements
						),
						ReturnStatement(
							ObjectCreationExpression(ParseTypeName($"{t.Name}Result"))
							.AddArgumentListArguments(
								Argument(
									MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										IdentifierName(ServerSDKStatusConstants.EnumName),
										IdentifierName(ServerSDKStatusConstants.Success))
								),
								Argument(
									IdentifierName(item)
								)
								,
								Argument(
									MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										IdentifierName(item),
										IdentifierName(key.Name)
									)
								)
							)
						)
					)
					.AddCatches(
						CatchClause()
						.WithDeclaration(
							CatchDeclaration(ParseTypeName("Exception"))
							.WithIdentifier(Identifier("ex"))
						)
						.WithBlock(
							Block(
								ReturnStatement(
									ObjectCreationExpression(ParseTypeName($"{t.Name}Result"))
									.AddArgumentListArguments(
										Argument(
											MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
												IdentifierName(ServerSDKStatusConstants.EnumName),
												IdentifierName(ServerSDKStatusConstants.Exception))
										),
										Argument(
											LiteralExpression(SyntaxKind.NullLiteralExpression)
										)
										,
										Argument(
											IdentifierName(keyName)
										),
										Argument(
											IdentifierName("ex")
										)
									)
								)
							)
						)
					)
				);

			list.Add(IfStatement(
				BinaryExpression(SyntaxKind.EqualsExpression,
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName(result),
						IdentifierName("StatusCode")
					),
					MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						IdentifierName("HttpStatusCode"),
						IdentifierName(httpBuilder.GetStatusCode("Ok"))
					)
				),
				block
			)
			.WithElse(ElseClause(Block()
				.AddStatements(
					ReturnStatement(
						ObjectCreationExpression(ParseTypeName($"{t.Name}Result"))
						.AddArgumentListArguments(
							Argument(
								MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
									IdentifierName(ServerSDKStatusConstants.EnumName),
									IdentifierName(ServerSDKStatusConstants.Error))
							),
							Argument(
								LiteralExpression(SyntaxKind.NullLiteralExpression)
							)
							,
							Argument(
								IdentifierName(keyName)
							)
						)
					)
				)
			)));

			return list.ToArray();
		}

		private MethodDeclarationSyntax createGetMethod(Type t, PropertyInfo[] pi)
		{

			var key = pi.GetKeyProperty();
			var body = new List<StatementSyntax>();
			body.AddRange(createCommonResultIfStatments(t, pi, $"{t.Name}Result"));
			body.AddRange(createGetCall(t, pi));

			var method = taskBuilder.AsyncMethod(
				TypeArgumentList().AddArguments(ParseTypeName($"{t.Name}Result")),
				$"Get{t.Name}Async",
				Block().AddStatements(body.ToArray())
				);

			method = method.AddParameterListParameters(
				Parameter(keyName).WithType(ParseTypeName(key.PropertyType.Name)));

			method = method.WithModifiers(method.Modifiers.Insert(0, Token(SyntaxKind.PublicKeyword)));

			return method;
		}

		private ClassDeclarationSyntax addType(PropertyWithName pwn, ClassDeclarationSyntax @class)
		{
			var tn = ParseTypeName(pwn.Type.Name);
			var pi = pwn.Type.GetProperties();

			var method = createGetMethod(pwn.Type, pi)
				.WithLeadingTrivia(Trivia(RegionDirectiveTrivia(true)
					.WithEndOfDirectiveToken(
						Token(TriviaList().Add(PreprocessingMessage(pwn.Type.Name)),
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

			/*cu.NormalizeWhitespace("\t", true).WriteTo(writer);
			writer.WriteLine();
			writer.WriteLine();*/

		}

		public void Generate(IList<PropertyWithName> props, String baseSaveDirectory, RunConfig config)
		{
			var dir = Path.Combine(baseSaveDirectory, config.Directory);
			if(!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}
			httpBuilder = config.HttpBuilder;
			taskBuilder = config.TaskBuilder;
			variables = config.Variables;

			var unit = CompilationUnit();
			unit = unit.AddUsing("System").WithLeadingTrivia(new SyntaxTriviaList().Add(GeneratorBase.GetLicenseComment()));
			unit = unit.AddUsing("System.Threading.Tasks");
			unit = unit.AddUsing("Newtonsoft.Json.Linq");
			if(String.Compare(variables.GetValue("IsUWP"), "1") == 0)
			{
				unit = unit.AddUsing("Windows.Foundation");
				unit = unit.AddUsing("System.Runtime.InteropServices");
			}
			unit = unit.AddUsings(config.HttpBuilder.Namespace);

			var names = NamespaceDeclaration(IdentifierName("Sannel.House.ServerSDK"));

			var @class = ClassDeclaration("ServerContext")
				.AddModifiers(
					Token(SyntaxKind.PublicKeyword),
					Token(SyntaxKind.PartialKeyword)
				);

			foreach(var prop in props)
			{
				@class = addType(prop, @class);
			}

			names = names.AddMembers(@class);

			unit = unit.AddMembers(names);

			unit = unit.NormalizeWhitespace("\t", true);
			using(var writer = new StreamWriter(File.OpenWrite(Path.Combine(dir, config.FileName))))
			{
				unit.WriteTo(writer);
			}
		}
	}
}
