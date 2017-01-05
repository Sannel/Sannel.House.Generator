﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using System.Reflection;
using Sannel.House.Generator.Common;

namespace Sannel.House.Generator.Generators
{
	public class ControllerGenerator : GeneratorBase
	{
		public ControllerGenerator()
		{

		}
		
		private MethodDeclarationSyntax generateGetMethod(String propertyName, Type t)
		{
			var method = SF.MethodDeclaration(SF.GenericName("IEnumerable").AddTypeArgumentListArguments(SF.ParseTypeName(t.Name)), "Get")
				.AddModifiers(SF.Token(SyntaxKind.PublicKeyword));

			var props = t.GetProperties();

			var forward = true;

			var dm = props.GetSortProperty(out forward);

			if (dm != null)
			{
				var rStatement = SF.ReturnStatement(
					SF.InvocationExpression(
						SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
							SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
								SF.IdentifierName("context"),
								SF.IdentifierName(propertyName)
							),
							SF.IdentifierName((forward) ? "OrderBy" : "OrderByDescending")))
						.AddArgumentListArguments(
							SF.Argument(
								SF.SimpleLambdaExpression(
									SF.Parameter(SF.Identifier("i")),
									SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										SF.IdentifierName("i"),
										SF.IdentifierName(dm.Name)
										)
									)
								)
						));
				method = method.AddBodyStatements(rStatement);
			}
			else
			{
				var rStatement = SF.ReturnStatement(SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					SF.IdentifierName("context"),
					SF.IdentifierName(propertyName)));
				method = method.AddBodyStatements(rStatement);
			}

			return method;
		}

		private MethodDeclarationSyntax generateGetWithIdMethod(String propertyName, Type t)
		{
			var props = t.GetProperties();

			var key = props.GetKeyProperty();
			if (key == null)
			{
				return null;
			}

			var method = SF.MethodDeclaration(SF.ParseTypeName(t.Name), "Get")
				.AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
				.AddParameterListParameters(
					SF.Parameter(SF.Identifier("id")).WithType(SF.ParseTypeName(key.PropertyType.Name))
				)
				.WithAttributeLists(
					new SyntaxList<AttributeListSyntax>().Add(
						SF.AttributeList().AddAttributes(
							SF.Attribute(SF.IdentifierName("HttpGet"))
							.AddArgumentListArguments(
								SF.AttributeArgument(SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal("{id}")))
							)
						)
					)
				);

			var rStatement = SF.ReturnStatement(
				SF.InvocationExpression(
					SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
							SF.IdentifierName("context"),
							SF.IdentifierName(propertyName)
						),
						SF.IdentifierName("FirstOrDefault")))
					.AddArgumentListArguments(
						SF.Argument(
							SF.SimpleLambdaExpression(
								SF.Parameter(SF.Identifier("i")),
								SF.BinaryExpression(SyntaxKind.EqualsExpression,
									SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
										SF.IdentifierName("i"),
										SF.IdentifierName(key.Name)
										),
									SF.IdentifierName("id")
									)
								)
							)
					));
			method = method.AddBodyStatements(rStatement);


			return method;

		}

		private MethodDeclarationSyntax generatePostMethod(String propertyName, Type t)
		{
			var props = t.GetProperties();
			var data = SF.Identifier("data");

			var key = props.GetKeyProperty();
			if(key == null)
			{
				return null;
			}

			var method = SF.MethodDeclaration(SF.GenericName("Result")
					.AddTypeArgumentListArguments(SF.ParseTypeName(t.Name))
					, "Post")
				.AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
				.AddParameterListParameters(
					SF.Parameter(data).WithType(SF.ParseTypeName(t.Name))
				)
				.WithAttributeLists(
					new SyntaxList<AttributeListSyntax>().Add(
						SF.AttributeList().AddAttributes(
							SF.Attribute(SF.IdentifierName("HttpPost"))
						)
					)
				);

			method = method.WithBody(SF.Block());

			return method;
		}

		protected override CompilationUnitSyntax internalGenerate(string propertyName, Type t)
		{
			var fileName = $"{t.Name}Controller";
			var unit = SF.CompilationUnit();

			unit = unit.AddUsings(SF.UsingDirective(SF.IdentifierName("System"))).WithLeadingTrivia(GetLicenseComment());
			unit = unit.AddUsings(SF.UsingDirective(SF.IdentifierName("System.Collections.Generic")));
			unit = unit.AddUsings(SF.UsingDirective(SF.IdentifierName("System.Linq")));
			unit = unit.AddUsings(SF.UsingDirective(SF.IdentifierName("System.Threading.Tasks")));
			unit = unit.AddUsings(SF.UsingDirective(SF.IdentifierName("Microsoft.AspNetCore.Mvc")));
			unit = unit.AddUsings(SF.UsingDirective(SF.IdentifierName("Sannel.House.Web.Base.Models")));
			unit = unit.AddUsings(SF.UsingDirective(SF.IdentifierName("Sannel.House.Web.Base.Interfaces")));

			var @class = SF.ClassDeclaration(fileName).AddModifiers(SF.Token(SyntaxKind.PublicKeyword)).AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("Controller")));

			@class = @class.AddAttributeLists(SF.AttributeList().AddAttributes(SF.Attribute(
					SF.IdentifierName("Route")
				).AddArgumentListArguments(
					SF.AttributeArgument(
						SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal("api/[controller]"))
					)
				)));


			@class = @class.AddMembers(generateGetMethod(propertyName, t));
			var get2 = generateGetWithIdMethod(propertyName, t);
			if (get2 != null)
			{
				@class = @class.AddMembers(get2);
			}

			var post = generatePostMethod(propertyName, t);
			if(post != null)
			{
				@class = @class.AddMembers(post);
			}


			var namesp = SF.NamespaceDeclaration(SF.ParseName("Sannel.House.Web.Controllers.api"));
			namesp = namesp.AddMembers(@class);
			var syntax = unit.AddMembers(namesp);
			return syntax;
		}
	}
}
