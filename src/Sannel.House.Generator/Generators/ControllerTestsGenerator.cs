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
	public class ControllerTestsGenerator : GeneratorBase
	{
		private StatementSyntax[] generateCompare(SyntaxToken expected, SyntaxToken actual, PropertyInfo[] props)
		{
			List<StatementSyntax> statements = new List<StatementSyntax>();
			var isfirst = true;
			foreach (var prop in props)
			{
				if (!prop.ShouldIgnore())
				{
					var exp = SF.ExpressionStatement(
						TestBuilder.AssertAreEqual(
							Extensions.MemberAccess(expected.Text, prop.Name),
							Extensions.MemberAccess(actual.Text, prop.Name)
						)
					);
					if (isfirst)
					{
						exp = exp.WithLeadingTrivia(SF.Comment($"// {expected.Text} -> {actual.Text}"));
						isfirst = false;
					}

					statements.Add(exp);
				}
			}

			return statements.ToArray();
		}

		private BlockSyntax generateSeeds(Type t, SyntaxToken context, String propertyName, out SyntaxToken var1, out SyntaxToken var2, out SyntaxToken var3, out PropertyInfo[] props)
		{
			var1 = SF.Identifier("var1");
			var2 = SF.Identifier("var2");
			var3 = SF.Identifier("var3");
			props = t.GetProperties();

			var blocks = SF.Block();

			var variable1 = Extensions.VariableDeclaration(var1.Text, t.Name, SF.ArgumentList());
			blocks = blocks.AddStatements(SF.LocalDeclarationStatement(variable1));
			var variable2 = Extensions.VariableDeclaration(var2.Text, t.Name, SF.ArgumentList());
			blocks = blocks.AddStatements(SF.LocalDeclarationStatement(variable2));
			var variable3 = Extensions.VariableDeclaration(var3.Text, t.Name, SF.ArgumentList());
			blocks = blocks.AddStatements(SF.LocalDeclarationStatement(variable3));

			var rand = new Random();
			List<ExpressionStatementSyntax> var1Sets = new List<ExpressionStatementSyntax>();
			List<ExpressionStatementSyntax> var2Sets = new List<ExpressionStatementSyntax>();
			List<ExpressionStatementSyntax> var3Sets = new List<ExpressionStatementSyntax>();

			foreach (var p in props)
			{
				if (!p.ShouldIgnore())
				{
					var1Sets.Add(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var1), p.Name, rand.LiteralForProperty(p.PropertyType, p.Name))));
					var2Sets.Add(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var2), p.Name, rand.LiteralForProperty(p.PropertyType, p.Name))));
					var3Sets.Add(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var3), p.Name, rand.LiteralForProperty(p.PropertyType, p.Name))));
				}
			}
			var1Sets[0] = var1Sets[0].WithLeadingTrivia(SF.Comment("//var1"));
			var2Sets[0] = var2Sets[0].WithLeadingTrivia(SF.Comment("//var2"));
			var3Sets[0] = var3Sets[0].WithLeadingTrivia(SF.Comment("//var3"));

			blocks = blocks.AddStatements(var1Sets.ToArray());
			blocks = blocks.AddStatements(var2Sets.ToArray());
			blocks = blocks.AddStatements(var3Sets.ToArray());

			bool isForward = false;
			var prop = props.GetSortProperty(out isForward);
			if (prop != null)
			{
				if (isForward)
				{
					int i = 1;
					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var3), prop.Name, i.ToLiteral()))
						.WithLeadingTrivia(SF.Comment("//Fix Order")));
					i++;
					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var2), prop.Name, i.ToLiteral())));
					i++;
					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var1), prop.Name, i.ToLiteral())));
				}
				else
				{
					String dtType = "DateTimeOffset";
					if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
					{
						dtType = "DateTime";
					}
					var order = SF.Identifier("order");
					blocks = blocks.AddStatements(SF.LocalDeclarationStatement(Extensions.VariableDeclaration(order.Text, SF.EqualsValueClause(Extensions.MemberAccess(dtType, "Now"))))
						.WithLeadingTrivia(SF.Comment("//Fix Order")));

					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var3), prop.Name, SF.IdentifierName(order))));

					blocks = blocks.AddStatements(SF.ExpressionStatement(
						Extensions.SetPropertyValue(SF.IdentifierName(var2), prop.Name, SF.InvocationExpression(Extensions.MemberAccess(order.Text, "AddDays"))
							.AddArgumentListArguments(SF.Argument((-1).ToLiteral())))));
					blocks = blocks.AddStatements(SF.ExpressionStatement(
						Extensions.SetPropertyValue(SF.IdentifierName(var1), prop.Name, SF.InvocationExpression(Extensions.MemberAccess(order.Text, "AddDays"))
							.AddArgumentListArguments(SF.Argument((-2).ToLiteral())))));
				}
			}

			blocks = blocks.AddStatements(
				SF.ExpressionStatement(SF.InvocationExpression(Extensions.MemberAccess(
				Extensions.MemberAccess(context.Text, propertyName),
				SF.IdentifierName("Add"))).WithArgumentList(
					SF.ArgumentList().AddArgument(var1.Text))
					).WithLeadingTrivia(SF.Comment("// Add and save entities")),
				SF.ExpressionStatement(
					SF.InvocationExpression(
						Extensions.MemberAccess(
							Extensions.MemberAccess(
								context.Text,
								propertyName
							),
							SF.IdentifierName("Add")
						)
					)
					.WithArgumentList(
						SF.ArgumentList()
							.AddArgument(var2.Text)
					)
				),
				SF.ExpressionStatement(
					SF.InvocationExpression(
						Extensions.MemberAccess(
							Extensions.MemberAccess(
								context.Text,
								propertyName
							),
							SF.IdentifierName("Add")
						)
					)
					.WithArgumentList(
						SF.ArgumentList()
							.AddArgument(var3.Text)
					)
				),
				SF.ExpressionStatement(
					SF.InvocationExpression(
						Extensions.MemberAccess(
							context.Text,
							"SaveChanges"
						)
					)
				)
					);

			return blocks;
		}

		private MethodDeclarationSyntax generateGetTest(String controllerName, String propertyName, Type t)
		{
			var context = SF.Identifier("context");
			var controller = SF.Identifier("controller");
			var method = SF.MethodDeclaration(SF.ParseTypeName("void"), "GetTest")
				.AddModifiers(SF.Token(SyntaxKind.PublicKeyword));

			var att = TestBuilder.GetMethodAttribute();
			if(att != null)
			{
				method = method.AddAttributeLists(
					SF.AttributeList().AddAttributes(att)
				);
			}

			SyntaxToken var1, var2, var3;
			PropertyInfo[] props;
			var blocks = generateSeeds(t, context, propertyName, out var1, out var2, out var3, out props);

			var results = SF.Identifier("results");
			var list = SF.Identifier("list");
			blocks = blocks.AddStatements(SF.LocalDeclarationStatement(
				Extensions.VariableDeclaration(
					results.Text,
					SF.EqualsValueClause(
						SF.InvocationExpression(
							Extensions.MemberAccess(
								SF.IdentifierName(controller),
								SF.IdentifierName("Get")
							)
						)
					)
				).WithLeadingTrivia(SF.Comment("//call get method"))
				),
				SF.ExpressionStatement(
					TestBuilder.AssertIsNotNull(SF.IdentifierName(results))
				),
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						list.Text,
						SF.EqualsValueClause(
							SF.InvocationExpression(
								Extensions.MemberAccess(
									SF.IdentifierName(results),
									SF.IdentifierName("ToList")
								)
							)
						)
					)
				)
				);

			blocks = blocks.AddStatements(SF.ExpressionStatement(
				TestBuilder.AssertAreEqual(3.ToLiteral(), Extensions.MemberAccess(list.Text, "Count"))
			));

			var one = SF.Identifier("one");
			blocks = blocks.AddStatements(SF.LocalDeclarationStatement(
				Extensions.VariableDeclaration(one.Text,
					SF.EqualsValueClause(
						SF.ElementAccessExpression(SF.IdentifierName(list))
						.WithArgumentList(SF.BracketedArgumentList()
							.AddArguments(
								SF.Argument(0.ToLiteral())
							)
						)
					)
				)
			));
			blocks = blocks.AddStatements(generateCompare(var3, one, props));

			var two = SF.Identifier("two");
			blocks = blocks.AddStatements(SF.LocalDeclarationStatement(
				Extensions.VariableDeclaration(two.Text,
					SF.EqualsValueClause(
						SF.ElementAccessExpression(SF.IdentifierName(list))
						.WithArgumentList(SF.BracketedArgumentList()
							.AddArguments(
								SF.Argument(1.ToLiteral())
							)
						)
					)
				)
			));
			blocks = blocks.AddStatements(generateCompare(var2, two, props));
			var three = SF.Identifier("three");
			blocks = blocks.AddStatements(SF.LocalDeclarationStatement(
				Extensions.VariableDeclaration(three.Text,
					SF.EqualsValueClause(
						SF.ElementAccessExpression(SF.IdentifierName(list))
						.WithArgumentList(SF.BracketedArgumentList()
							.AddArguments(
								SF.Argument(2.ToLiteral())
							)
						)
					)
				)
			));
			blocks = blocks.AddStatements(generateCompare(var1, three, props));

			var @using2 = SF.UsingStatement(blocks)
				.WithDeclaration(Extensions.VariableDeclaration(controller.Text,
					controllerName,
					SF.ArgumentList().AddArgument(context.Text)));

			var @using = SF.UsingStatement(SF.Block(@using2))
				.WithDeclaration(Extensions.VariableDeclaration(context.Text, "MockDataContext", SF.ArgumentList(), "IDataContext"));

			method = method.AddBodyStatements(@using);

			return method;
		}

		private MethodDeclarationSyntax generateGetByIdTest(String controllerName, String propertyName, Type t)
		{
			var context = SF.Identifier("context");
			var controller = SF.Identifier("controller");
			var method = SF.MethodDeclaration(SF.ParseTypeName("void"), "GetWithIdTest")
				.AddModifiers(SF.Token(SyntaxKind.PublicKeyword));

			var att = TestBuilder.GetMethodAttribute();
			if (att != null)
			{
				method = method.AddAttributeLists(SF.AttributeList().AddAttributes(att));
			}

			SyntaxToken var1, var2, var3;
			PropertyInfo[] props;
			var blocks = generateSeeds(t, context, propertyName, out var1, out var2, out var3, out props);

			var prop = props.GetKeyProperty();
			var actual = SF.Identifier("actual");
			blocks = blocks.AddStatements(
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						actual.Text,
						SF.EqualsValueClause(
							SF.InvocationExpression(
								Extensions.MemberAccess(
									controller.Text,
									"Get"
								)
							).WithArgumentList(
								SF.ArgumentList().AddArguments(
									SF.Argument(Extensions.MemberAccess(var1.Text, prop.Name))
								)
							)
						)
					)
				).WithLeadingTrivia(SF.Comment("// verify"))
			);

			blocks = blocks.AddStatements(SF.ExpressionStatement(
				TestBuilder.AssertIsNotNull(Extensions.MemberAccess(actual.Text, prop.Name))
			));

			blocks = blocks.AddStatements(generateCompare(var1, actual, props));

			blocks = blocks.AddStatements(
				SF.ExpressionStatement(
					SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						SF.IdentifierName(actual),
						SF.InvocationExpression(
							Extensions.MemberAccess(
								controller.Text,
								"Get"
							)
						).WithArgumentList(
							SF.ArgumentList()
							.AddArguments(
									SF.Argument(Extensions.MemberAccess(var2.Text, prop.Name))
							)
						)
					)
				).WithLeadingTrivia(SF.Comment($"// Verify {var2.Text}"))
			);

			blocks = blocks.AddStatements(
				SF.ExpressionStatement(
					Extensions.MemberAccess(actual.Text, prop.Name)
				)
			);
			blocks = blocks.AddStatements(generateCompare(var2, actual, props));

			blocks = blocks.AddStatements(
				SF.ExpressionStatement(
					SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						SF.IdentifierName(actual),
						SF.InvocationExpression(
							Extensions.MemberAccess(
								controller.Text,
								"Get"
							)
						).WithArgumentList(
							SF.ArgumentList()
							.AddArguments(
									SF.Argument(Extensions.MemberAccess(var3.Text, prop.Name))
							)
						)
					)
				).WithLeadingTrivia(SF.Comment($"// Verify {var3.Text}"))
			);

			blocks = blocks.AddStatements(
				SF.ExpressionStatement(
					Extensions.MemberAccess(actual.Text, prop.Name)
				)
			);
			blocks = blocks.AddStatements(generateCompare(var3, actual, props));

			var @using2 = SF.UsingStatement(blocks)
				.WithDeclaration(Extensions.VariableDeclaration(controller.Text,
					controllerName,
					SF.ArgumentList().AddArgument(context.Text)));

			var @using = SF.UsingStatement(SF.Block(@using2))
				.WithDeclaration(Extensions.VariableDeclaration(context.Text, "MockDataContext", SF.ArgumentList(), "IDataContext"));

			method = method.AddBodyStatements(@using);

			return method;
		}

		protected override CompilationUnitSyntax internalGenerate(string propertyName, Type t)
		{
			var controllerName = $"{t.Name}Controller";
			var filename = $"{controllerName}Tests";
			var unit = SF.CompilationUnit();

			unit = unit.AddUsing("System").WithLeadingTrivia(GetLicenseComment());
			unit = unit.AddUsings(
					"NUnit.Framework",
					"Sannel.House.Web.Base.Interfaces",
					"Sannel.House.Web.Base.Models",
					"Sannel.House.Web.Controllers.api",
					"Sannel.House.Web.Mocks",
					"System.Collections.Generic",
					"System.Linq",
					"System.Threading.Tasks");

			var @class = SF.ClassDeclaration(filename)
				.AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
				.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("TestBase")));

			var att = TestBuilder.GetClassAttribute();
			if (att != null)
			{
				@class = @class.AddAttributeLists(
					SF.AttributeList().AddAttributes(att)
				);
			}

			@class = @class.AddMembers(generateGetTest(controllerName, propertyName, t));
			@class = @class.AddMembers(generateGetByIdTest(controllerName, propertyName, t));

			return unit.AddMembers(SF.NamespaceDeclaration(SF.IdentifierName("Sannel.House.Web.Tests")).AddMembers(@class));
		}
	}
}
