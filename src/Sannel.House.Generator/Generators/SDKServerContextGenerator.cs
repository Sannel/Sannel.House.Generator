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
	public class SDKServerContextGenerator : ICombinedGenerator
	{
		private SyntaxToken keyName = Identifier("key");
		private SyntaxToken helperName = Identifier("helper");
		private IHttpClientBuilder httpBuilder;
		private ITaskBuilder taskBuilder;
		private Dictionary<String, String> variables;

		private ClassDeclarationSyntax addType(PropertyWithName prop, ClassDeclarationSyntax @class)
		{
			var t = prop.Type;

			var props = t.GetProperties();
			var key = props.GetKeyProperty();

			var subContext = PropertyDeclaration(ParseTypeName($"{prop.Type.Name}Context"), prop.PropertyName)
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddAccessorListAccessors(
					AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
					AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).AddModifiers(Token(SyntaxKind.PrivateKeyword)).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
				);


			return @class.AddMembers(subContext);
		}

		private ClassDeclarationSyntax addConstructor(IList<PropertyWithName> props, String name, ClassDeclarationSyntax @class)
		{
			var cons = ConstructorDeclaration(name)
				.AddModifiers(Token(SyntaxKind.PrivateKeyword));

			var blocks = Block();

			foreach(var prop in props)
			{
				blocks = blocks.AddStatements(
					ExpressionStatement(
						AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
							IdentifierName(prop.PropertyName),
							ObjectCreationExpression(
								ParseTypeName($"{prop.Type.Name}Context")
							).AddArgumentListArguments(
								Argument(
									ThisExpression()
								)
							)
						)
					)
				);
			}

			return @class.AddMembers(cons.WithBody(blocks));
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
			unit = unit.AddUsings(config.HttpBuilder.Namespace);

			var names = NamespaceDeclaration(IdentifierName("Sannel.House.ServerSDK"));

			var @class = ClassDeclaration("ServerContext")
				.AddModifiers(
					Token(SyntaxKind.PublicKeyword),
					Token(SyntaxKind.PartialKeyword)
				);

			@class = addConstructor(props, "ServerContext", @class);

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
