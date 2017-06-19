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
										sc.MemberAccess("Devices", "GetPagedAsync")
									).AddArgumentListArguments(
										Argument(1.ToLiteral()),
										Argument(20.ToLiteral())
									)
								)
							)
						)
					),
					ExpressionStatement(TestBuilder.AssertIsFalse(result.MemberAccess("Success"))),
					ExpressionStatement(TestBuilder.AssertIsNotNull(result.MemberAccess("Errors"))),
					ExpressionStatement(TestBuilder.AssertAreEqual(1.ToLiteral(), result.MemberAccess("Errors", "Count"))),
					ExpressionStatement(TestBuilder.AssertAreEqual(
						"Errors".MemberAccess("ServerContext_PleaseLogin"),
						ElementAccessExpression(result.MemberAccess("Errors"))
						.AddArgumentListArguments(
							Argument(0.ToLiteral())
						)
						))
				);

			/*					var result = await sc.Devices.GetPagedAsync(1, 20);
					Assert.False(result.Success);
					Assert.NotNull(result.Errors);
					Assert.Equal(1, result.Errors.Count);
					Assert.Equal(Errors.ServerContext_PleaseLogin, result.Errors[0]);
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
						EqualsValueClause(ObjectCreationExpression(ParseTypeName("MockHttpClient")))
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
				//@class = @class.AddMembers(generateGetByIdTest(controllerName, propertyName, t));
			}
			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Post))
			{
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
