using Sannel.House.Generator.Common;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using Newtonsoft.Json;

namespace Sannel.House.Generator.Generators
{
	public class SDKModelsGenerator : GeneratorBase
	{
		protected List<MemberDeclarationSyntax> generateProperty(PropertyInfo pi)
		{
			var sections = new List<MemberDeclarationSyntax>();
			var field = Identifier($"_{pi.Name}");
			sections.Add(
				FieldDeclaration(
					VariableDeclaration(pi.GetTypeSyntax())
					.AddVariables(
						VariableDeclarator(field)
						.WithInitializer(
							EqualsValueClause(pi.GetTypeSyntax().GetDefaultValue())
						)
					)
				).AddModifiers(Token(SyntaxKind.PrivateKeyword))
			);

			var list = AttributeList();

			foreach(var attribute in pi.GetCustomAttributes())
			{
				if (attribute is JsonPropertyAttribute jProp)
				{
					list.AddAttributes(Attribute(IdentifierName("JsonProperty"))
						.AddArgumentListArguments(AttributeArgument(jProp.PropertyName.ToLiteral())));
				}
			}

			var prop = PropertyDeclaration(pi.GetTypeSyntax(), pi.Name)
				.AddModifiers(Token(SyntaxKind.PublicKeyword))
				.AddAccessorListAccessors(
					AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
					.WithExpressionBody(ArrowExpressionClause(field.ToIN())).WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
					AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
					.WithExpressionBody(
						ArrowExpressionClause(
							InvocationExpression(
								IdentifierName("Set")
							).AddArgumentListArguments(
								Argument(IdentifierName(field.Text)).WithRefOrOutKeyword(Token(SyntaxKind.RefKeyword)),
								Argument(IdentifierName("value"))
							)
						)
					).WithSemicolonToken(Token(SyntaxKind.SemicolonToken))
				);

			if(list.Attributes.Count > 0)
			{
				prop = prop.AddAttributeLists(list);
			}

			sections.Add(prop);


			return sections;
		}

		protected override CompilationUnitSyntax internalGenerate(string propertyName, Type t)
		{
			var unit = CompilationUnit();

			unit = unit.AddUsings("System").WithLeadingTrivia(GetLicenseComment());
			unit = unit.AddUsings("System.Threading.Tasks",
									"Sannel.House.ServerSDK.Results");

			var @class = ClassDeclaration(t.Name)
						.AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword))
						.AddBaseListTypes(SimpleBaseType(ParseTypeName("ModelBase")));

			var props = t.GetProperties();
			var body = new List<MemberDeclarationSyntax>();
			
			foreach(var prop in props)
			{
				if (!prop.ShouldIgnore())
				{
					body.AddRange(generateProperty(prop));
				}
			}

			@class = @class.AddMembers(body.ToArray());



			return unit.AddMembers(NamespaceDeclaration(IdentifierName("Sannel.House.ServerSDK.Models")).AddMembers(@class));
		}
	}
}
