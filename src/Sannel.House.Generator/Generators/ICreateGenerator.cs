using Sannel.House.Generator.Interfaces;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System;
using System.Collections.Generic;
using System.Text;
using Sannel.House.Generator.Common;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sannel.House.Generator.Generators
{
	public class ICreateGenerator : ICombinedGenerator
	{
		private InterfaceDeclarationSyntax addType(PropertyWithName pwn, InterfaceDeclarationSyntax @interface)
		{
			var method = MethodDeclaration(ParseTypeName($"I{pwn.Type.Name}"), $"Create{pwn.Type.Name}")
				.WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

			return @interface.AddMembers(method);
		}

		public void Generate(IList<PropertyWithName> props, string baseSaveDirectory, RunConfig config)
		{
			var dir = Path.Combine(baseSaveDirectory, config.Directory);
			if(!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			var unit = CompilationUnit();
			unit = unit.AddUsing("System").WithLeadingTrivia(new SyntaxTriviaList().Add(GeneratorBase.GetLicenseComment()));

			var names = NamespaceDeclaration(IdentifierName("Sannel.House.ServerSDK"));

			var @interface = InterfaceDeclaration("ICreateHelper")
				.AddModifiers(
					Token(SyntaxKind.PublicKeyword)
				);

			foreach(var prop in props)
			{
				@interface = addType(prop, @interface);
			}

			names = names.AddMembers(@interface);

			unit = unit.AddMembers(names);

			unit = unit.NormalizeWhitespace("\t", true);
			using(var writer = new StreamWriter(File.OpenWrite(Path.Combine(dir, config.FileName))))
			{
				unit.WriteTo(writer);
			}
		}
	}
}
