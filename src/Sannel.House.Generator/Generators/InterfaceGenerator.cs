using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using System.Reflection;
using Sannel.House.Generator.Common;

namespace Sannel.House.Generator.Generators
{
	public class InterfaceGenerator : GeneratorBase
	{
		protected override CompilationUnitSyntax internalGenerate(string propertyName, Type t)
		{
			var fileName = $"I{t.Name}";
			var unit = SF.CompilationUnit();

			unit = unit.AddUsing("System").WithLeadingTrivia(GetLicenseComment());
			unit = unit.AddUsings(
				"Newtonsoft.Json",
				"System");

			var @interface = SF.InterfaceDeclaration(fileName)
				.AddModifiers(SF.Token(SyntaxKind.PublicKeyword));

			var props = t.GetProperties();
			foreach(var prop in props)
			{
				if (!prop.ShouldIgnore())
				{
					@interface = @interface.AddMembers(
						SF.PropertyDeclaration(prop.GetTypeSyntax(), prop.Name)
						.WithAccessorList(
							SF.AccessorList()
							.AddAccessors(SF.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken)))
							.AddAccessors(SF.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken)))
						)
					);
				}
			}

			return unit.AddMembers(SF.NamespaceDeclaration(SF.IdentifierName("Sannel.House.ServerSDK")).AddMembers(@interface));
		}
	}
}
