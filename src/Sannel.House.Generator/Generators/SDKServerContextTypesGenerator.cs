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

		private MethodDeclarationSyntax generatePagedMethod(Type t)
		{
			var method = MethodDeclaration(GenericName("PagedResults").AddTypeArgumentListArguments(ParseTypeName(t.Name)), "GetPaged")
							.AddModifiers(Token(SyntaxKind.PublicKeyword));

			var blocks = Block(ReturnStatement(InvocationExpression(IdentifierName("GetPaged")).AddArgumentListArguments(Argument(1.ToLiteral()))));

			return method.WithBody(blocks);
		}

		private MethodDeclarationSyntax generatePagedMethodWithPage(Type t)
		{

			var method = MethodDeclaration(GenericName("PagedResults").AddTypeArgumentListArguments(ParseTypeName(t.Name)), "GetPaged")
							.AddModifiers(Token(SyntaxKind.PublicKeyword))
							.AddParameterListParameters(
								Parameter(Identifier("page")).WithType(ParseTypeName("int"))
							);

			var blocks = Block(ReturnStatement(InvocationExpression(IdentifierName("GetPaged"))
				.AddArgumentListArguments(
					Argument(IdentifierName("page")),
					Argument(10.ToLiteral())
				)));

			return method.WithBody(blocks);
		}

		protected override CompilationUnitSyntax internalGenerate(string propertyName, Type t)
		{
			var unit = CompilationUnit();

			unit = unit.AddUsings("System").WithLeadingTrivia(GetLicenseComment());
			unit = unit.AddUsings("System.Threading.Tasks",
									"Sannel.House.ServerSDK.Results");

			var @class = ClassDeclaration($"{t.Name}Context")
						.AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword))
						.AddBaseListTypes(SimpleBaseType(ParseTypeName("ContextBase")));

			@class = addConstructor(@class);


			var ti = t.GetTypeInfo();
			var ga = ti.GetCustomAttribute<GenerationAttribute>() ?? new GenerationAttribute();

			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Get))
			{
				@class = @class.AddMembers(generatePagedMethod(t));
				@class = @class.AddMembers(generatePagedMethodWithPage(t));
			}

			unit = unit.AddMembers(NamespaceDeclaration(IdentifierName("Sannel.House.ServerSDK.Context")).AddMembers(@class));
			return unit;
		}
	}
}
