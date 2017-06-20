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
using Sannel.House.Web.Base;

namespace Sannel.House.Generator.Generators
{
	public class ControllerTestsGenerator : GeneratorBase
	{
		private SyntaxToken wrapper = SF.Identifier("wrapper");
		private SyntaxToken context = SF.Identifier("context");
		private SyntaxToken controller = SF.Identifier("controller");
		private SyntaxToken logger = SF.Identifier("logger");
		private SyntaxToken logFactory = SF.Identifier("logFactory");

		private StatementSyntax[] generateCompare(SyntaxToken expected, SyntaxToken actual, PropertyInfo[] props)
		{
			List<StatementSyntax> statements = new List<StatementSyntax>();
			var isfirst = true;
			foreach (var prop in props)
			{
				if (!prop.ShouldIgnore())
				{
					var exp = SF.ExpressionStatement(
						TestBuilder.Equal(
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

		private BlockSyntax wrapBlocks(BlockSyntax blocks, String controllerName)
		{
			var @using2 = SF.UsingStatement(blocks)
				.WithDeclaration(Extensions.VariableDeclaration(controller.Text,
					controllerName,
					SF.ArgumentList().AddArgument(context.Text)
					.AddArgument(logger.Text)));

			var @using1Blocks = SF.Block(
									SF.LocalDeclarationStatement(
									Extensions.VariableDeclaration(context.Text, SF.EqualsValueClause(
										Extensions.MemberAccess(wrapper.Text, "Context")))),
									@using2
								);
			var @using = SF.UsingStatement(@using1Blocks)
				.WithDeclaration(Extensions.VariableDeclaration(wrapper.Text, "ContextWrapper", SF.ArgumentList().AddArguments(
					SF.Argument(SF.ThisExpression())
					), "var"));

			return SF.Block().AddStatements(
				SF.LocalDeclarationStatement(
						Extensions.VariableDeclaration(logFactory.Text,
							SF.EqualsValueClause(SF.ObjectCreationExpression(SF.ParseTypeName("LoggerFactory")).AddArgumentListArguments())
						)
					),
					SF.LocalDeclarationStatement(
						Extensions.VariableDeclaration(logger.Text,
							SF.EqualsValueClause(
								SF.InvocationExpression(
									Extensions.MemberAccess(
										SF.IdentifierName(logFactory),
										SF.GenericName(SF.Identifier("CreateLogger"))
										.AddTypeArgumentListArguments(
											SF.ParseTypeName(controllerName)
										)
									)
								)
							)
						)
					),
					@using
				);

		}

		private BlockSyntax generateSeeds(Type t, SyntaxToken context, String propertyName, out SyntaxToken var1, out SyntaxToken var2, out SyntaxToken var3, out PropertyInfo[] props)
		{
			var wrapper = SF.Identifier("wrapper");
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
					String dtType = "DateTime";
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
							wrapper.Text,
							"SaveChanges"
						)
					)
				)
					);

			

			return blocks;
		}

		private StatementSyntax[] generateSeedObject(String variableName, String typeName, PropertyInfo[] props, String ignorePropertyName, bool createNewObject=true)
		{
			Task.Delay(25).Wait(); // delay a little bit so we dont generate the same items
			var rand = new Random();
			List<ExpressionStatementSyntax> statements = new List<ExpressionStatementSyntax>();

			if (createNewObject)
			{
				statements.Add(
					SF.ExpressionStatement(
						SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
							SF.IdentifierName(variableName),
							SF.ObjectCreationExpression(SF.ParseTypeName(typeName))
							.AddArgumentListArguments()
						)
					));
			}

			foreach (var p in props)
			{
				if (!p.ShouldIgnore() && String.Compare(p.Name, ignorePropertyName, true) != 0)
				{
					if (p.HasAlwaysValue())
					{
						Object o = p.GetAlwaysValue();
						statements.Add(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(variableName), p.Name, o.LiteralForObject())));
					}
					else
					{
						statements.Add(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(variableName), p.Name, rand.LiteralForProperty(p.PropertyType, p.Name))));
					}
				}
			}

			if(statements.Count > 0)
			{
				statements[0] = statements[0].WithLeadingTrivia(SF.Comment($"// {ignorePropertyName}"));
			}

			return statements.ToArray();
		}

		private MethodDeclarationSyntax generateGetTest(String controllerName, String propertyName, Type t)
		{
			var method = SF.MethodDeclaration(SF.ParseTypeName("void"), "GetPagedTest")
				.AddModifiers(SF.Token(SyntaxKind.PublicKeyword));

			var att = TestBuilder.GetMethodAttribute();
			if(att != null)
			{
				method = method.AddAttributeLists(
					SF.AttributeList().AddAttributes(att)
				);
			}

			var createObj = SF.ObjectCreationExpression(SF.ParseTypeName(t.Name)).AddArgumentListArguments();

			SyntaxToken var1 = SF.Identifier("var1"), var2 = SF.Identifier("var2"), var3 = SF.Identifier("var3"), var4 = SF.Identifier("var4"), var5 = SF.Identifier("var5");
			PropertyInfo[] props = t.GetProperties();
			var blocks = SF.Block();

			blocks = blocks.AddStatements(
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(var1.Text, SF.EqualsValueClause(createObj))
				),
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(var2.Text, SF.EqualsValueClause(createObj))
				),
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(var3.Text, SF.EqualsValueClause(createObj))
				),
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(var4.Text, SF.EqualsValueClause(createObj))
				),
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(var5.Text, SF.EqualsValueClause(createObj))
				)
			);

			blocks = blocks.AddStatements(generateSeedObject(var1.Text, t.Name, props, "var1", false));
			blocks = blocks.AddStatements(generateSeedObject(var2.Text, t.Name, props, "var2", false));
			blocks = blocks.AddStatements(generateSeedObject(var3.Text, t.Name, props, "var3", false));
			blocks = blocks.AddStatements(generateSeedObject(var4.Text, t.Name, props, "var4", false));
			blocks = blocks.AddStatements(generateSeedObject(var5.Text, t.Name, props, "var5", false));

			var prop = props.GetSortProperty(out bool isForward);
			if(prop != null)
			{
				if (isForward)
				{
					int i = 1;
					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var5), prop.Name, i.ToLiteral())).WithLeadingTrivia(SF.Comment("// Fix order")));
					i++;
					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var4), prop.Name, i.ToLiteral())));
					i++;
					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var3), prop.Name, i.ToLiteral())));
					i++;
					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var2), prop.Name, i.ToLiteral())));
					i++;
					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var1), prop.Name, i.ToLiteral())));
				}
				else
				{
					var order = SF.Identifier("order");
					blocks = blocks.AddStatements(SF.LocalDeclarationStatement(Extensions.VariableDeclaration(order.Text, SF.EqualsValueClause("DateTime".MemberAccess("Now"))))).WithLeadingTrivia(SF.Comment("// Fix order"));

					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var1), prop.Name, SF.IdentifierName(order))));
					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var2), prop.Name, SF.InvocationExpression(order.Text.MemberAccess("AddDays")).AddArgumentListArguments(SF.Argument((-1).ToLiteral())))));
					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var3), prop.Name, SF.InvocationExpression(order.Text.MemberAccess("AddDays")).AddArgumentListArguments(SF.Argument((-2).ToLiteral())))));
					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var4), prop.Name, SF.InvocationExpression(order.Text.MemberAccess("AddDays")).AddArgumentListArguments(SF.Argument((-3).ToLiteral())))));
					blocks = blocks.AddStatements(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(var5), prop.Name, SF.InvocationExpression(order.Text.MemberAccess("AddDays")).AddArgumentListArguments(SF.Argument((-4).ToLiteral())))));
				}
			}

			blocks = blocks.AddStatements(SF.ExpressionStatement(SF.InvocationExpression(context.Text.MemberAccess(propertyName, "Add")).AddArgumentListArguments(SF.Argument(SF.IdentifierName(var1))).WithLeadingTrivia(SF.Comment("// Add to database"))));
			blocks = blocks.AddStatements(SF.ExpressionStatement(SF.InvocationExpression(context.Text.MemberAccess(propertyName, "Add")).AddArgumentListArguments(SF.Argument(SF.IdentifierName(var2)))));
			blocks = blocks.AddStatements(SF.ExpressionStatement(SF.InvocationExpression(context.Text.MemberAccess(propertyName, "Add")).AddArgumentListArguments(SF.Argument(SF.IdentifierName(var3)))));
			blocks = blocks.AddStatements(SF.ExpressionStatement(SF.InvocationExpression(context.Text.MemberAccess(propertyName, "Add")).AddArgumentListArguments(SF.Argument(SF.IdentifierName(var4)))));
			blocks = blocks.AddStatements(SF.ExpressionStatement(SF.InvocationExpression(context.Text.MemberAccess(propertyName, "Add")).AddArgumentListArguments(SF.Argument(SF.IdentifierName(var5)))));
			blocks = blocks.AddStatements(SF.ExpressionStatement(SF.InvocationExpression(wrapper.Text.MemberAccess("SaveChanges")).AddArgumentListArguments()));

			var paged = SF.Identifier("paged");
			// Error Tests
			blocks = blocks.AddStatements(
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						paged.Text, 
						SF.EqualsValueClause(SF.InvocationExpression(controller.Text.MemberAccess("GetPaged"))
							.AddArgumentListArguments(
								SF.Argument(0.ToLiteral()),
								SF.Argument(2.ToLiteral())
							)
						)
					).WithLeadingTrivia(SF.Comment("// Call with invalid page number"))
				),
				SF.ExpressionStatement(
					TestBuilder.NotNull(SF.IdentifierName(paged))
				),
				SF.ExpressionStatement(
					TestBuilder.False(paged.Text.MemberAccess("Success"))
				),
				SF.ExpressionStatement(
					TestBuilder.Equal(1.ToLiteral(), paged.MemberAccess("Errors", "Count"))
				),
				SF.ExpressionStatement(
					TestBuilder.Equal("Page must be 1 or greater".ToLiteral(),
						SF.ElementAccessExpression(
							paged.MemberAccess("Errors")
						).AddArgumentListArguments(
							SF.Argument(0.ToLiteral())
						)
					)
				)
			);

			blocks = blocks.AddStatements(
				SF.ExpressionStatement(
					SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						paged.ToIN(),
						SF.InvocationExpression(
							controller.MemberAccess("GetPaged")
						).AddArgumentListArguments(
							SF.Argument(1.ToLiteral()),
							SF.Argument(0.ToLiteral())
						)
					).WithLeadingTrivia(SF.Comment("// Invalid PageSize"))
				),
				SF.ExpressionStatement(
					TestBuilder.NotNull(paged.ToIN())
				),
				SF.ExpressionStatement(
					TestBuilder.False(paged.MemberAccess("Success"))
				),
				SF.ExpressionStatement(
					TestBuilder.Equal(1.ToLiteral(), paged.MemberAccess("Errors", "Count"))
				),
				SF.ExpressionStatement(
					TestBuilder.Equal("PageSize must be 1 or greater".ToLiteral(),
						SF.ElementAccessExpression(
							paged.MemberAccess("Errors")
						).AddArgumentListArguments(
							SF.Argument(0.ToLiteral())
						)
					)
				)
			);
			/*paged = controller.GetPaged(1, 2);
					Assert.NotNull(paged);
					Assert.True(paged.Success);
					Assert.Equal(5, paged.TotalResults);
					Assert.Equal(2, paged.PageSize);
					Assert.Equal(1, paged.CurrentPage);
					Assert.NotNull(paged.Data);
					var list = paged.Data.ToList();
					Assert.Equal(2, list.Count);
					var actual = list[0]; // var1
					// common test
					actual = list[1]; // var2
					// common test*/
			var list = SF.Identifier("list");
			var actual = SF.Identifier("actual");

			SyntaxToken aVar1, aVar2, aVar3, aVar4, aVar5;

			if (isForward)
			{
				aVar1 = var5;
				aVar2 = var4;
				aVar3 = var3;
				aVar4 = var2;
				aVar5 = var1;
			}
			else
			{
				aVar1 = var1;
				aVar2 = var2;
				aVar3 = var3;
				aVar4 = var4;
				aVar5 = var5;
			}


			blocks = blocks.AddStatements(
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					paged.ToIN(),
					controller.MemberAccess("GetPaged")
						.Invoke(
							1.ToArgument(),
							2.ToArgument()
						)
				).WithLeadingTrivia(SF.Comment("// Success Tests")).ToStatement(),
				TestBuilder.NotNull(
					paged.ToIN()
				).ToStatement(),
				TestBuilder.True(
					paged.MemberAccess("Success")
				).ToStatement(),
				TestBuilder.Equal(
					5.ToLiteral(),
					paged.MemberAccess("TotalResults")
				).ToStatement(),
				TestBuilder.Equal(
					2.ToLiteral(),
					paged.MemberAccess("PageSize")
				).ToStatement(),
				TestBuilder.Equal(
					1.ToLiteral(),
					paged.MemberAccess("CurrentPage")
				).ToStatement(),
				TestBuilder.NotNull(
					paged.MemberAccess("Data")
				).ToStatement(),
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						list.Text,
						SF.EqualsValueClause(
							paged.MemberAccess("Data", "ToList").Invoke()
						)
					)
				),
				TestBuilder.Equal(2.ToLiteral(), list.MemberAccess("Count")).ToStatement(),
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						actual.Text,
						SF.EqualsValueClause(
						SF.ElementAccessExpression(
							list.ToIN()
						).AddArgumentListArguments(0.ToArgument())
						)
					).WithLeadingTrivia(SF.Comment($"// {aVar1}"))
				)
			);

			blocks = blocks.AddStatements(generateCompare(aVar1, actual, props));
			blocks = blocks.AddStatements(
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					actual.ToIN(),
					SF.ElementAccessExpression(
						list.ToIN()
					).AddArgumentListArguments(1.ToArgument())
				).WithLeadingTrivia(SF.Comment($"// {aVar2}")).ToStatement()
			);
			blocks = blocks.AddStatements(generateCompare(aVar2, actual, props));

			blocks = blocks.AddStatements(
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					paged.ToIN(),
					controller.MemberAccess("GetPaged")
						.Invoke(
							2.ToArgument(),
							2.ToArgument()
						)
				).WithLeadingTrivia(SF.Comment("// Success Tests")).ToStatement(),
				TestBuilder.NotNull(
					paged.ToIN()
				).ToStatement(),
				TestBuilder.True(
					paged.MemberAccess("Success")
				).ToStatement(),
				TestBuilder.Equal(
					5.ToLiteral(),
					paged.MemberAccess("TotalResults")
				).ToStatement(),
				TestBuilder.Equal(
					2.ToLiteral(),
					paged.MemberAccess("PageSize")
				).ToStatement(),
				TestBuilder.Equal(
					2.ToLiteral(),
					paged.MemberAccess("CurrentPage")
				).ToStatement(),
				TestBuilder.NotNull(
					paged.MemberAccess("Data")
				).ToStatement(),
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					list.ToIN(),
					paged.MemberAccess("Data", "ToList").Invoke()
				).ToStatement(),
				TestBuilder.Equal(2.ToLiteral(), list.MemberAccess("Count")).ToStatement(),
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					actual.ToIN(),
					SF.ElementAccessExpression(
						list.ToIN()
					).AddArgumentListArguments(
						0.ToArgument()
					)
				).WithLeadingTrivia(SF.Comment($"// {aVar3}")).ToStatement()
			);

			blocks = blocks.AddStatements(generateCompare(aVar3, actual, props));
			blocks = blocks.AddStatements(
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					actual.ToIN(),
					SF.ElementAccessExpression(
						list.ToIN()
					).AddArgumentListArguments(1.ToArgument())
				).WithLeadingTrivia(SF.Comment($"// {aVar4}")).ToStatement()
			);
			blocks = blocks.AddStatements(generateCompare(aVar4, actual, props));

			blocks = blocks.AddStatements(
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					paged.ToIN(),
					controller.MemberAccess("GetPaged")
						.Invoke(
							3.ToArgument(),
							2.ToArgument()
						)
				).WithLeadingTrivia(SF.Comment("// Success Tests")).ToStatement(),
				TestBuilder.NotNull(
					paged.ToIN()
				).ToStatement(),
				TestBuilder.True(
					paged.MemberAccess("Success")
				).ToStatement(),
				TestBuilder.Equal(
					5.ToLiteral(),
					paged.MemberAccess("TotalResults")
				).ToStatement(),
				TestBuilder.Equal(
					2.ToLiteral(),
					paged.MemberAccess("PageSize")
				).ToStatement(),
				TestBuilder.Equal(
					3.ToLiteral(),
					paged.MemberAccess("CurrentPage")
				).ToStatement(),
				TestBuilder.NotNull(
					paged.MemberAccess("Data")
				).ToStatement(),
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					list.ToIN(),
					paged.MemberAccess("Data", "ToList").Invoke()
				).ToStatement(),
				TestBuilder.Equal(1.ToLiteral(), list.MemberAccess("Count")).ToStatement(),
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					actual.ToIN(),
					SF.ElementAccessExpression(
						list.ToIN()
					).AddArgumentListArguments(
						0.ToArgument()
					)
				).WithLeadingTrivia(SF.Comment($"// {aVar5}")).ToStatement()
			);

			blocks = blocks.AddStatements(generateCompare(aVar5, actual, props));

			method = method.WithBody(wrapBlocks(blocks, controllerName));

			return method;
		}

		private MethodDeclarationSyntax generateGetByIdTest(String controllerName, String propertyName, Type t)
		{
			var wrapper = SF.Identifier("wrapper");
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
			var results = SF.Identifier("results");
			var actual = SF.Identifier("actual");
			/*						var results = controller.Get(var1.Id);
						Assert.NotNull(results);
						Assert.True(results.Success);
						var actual = results.Data;
*/
			blocks = blocks.AddStatements(
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						results.Text,
						SF.EqualsValueClause(
							controller.MemberAccess("Get")
							.Invoke(
								SF.Argument(var1.MemberAccess(prop.Name))
							)
						)
					)
				).WithLeadingTrivia(SF.Comment("// verify")),
				TestBuilder.NotNull(results.ToIN()).ToStatement(),
				TestBuilder.True(results.MemberAccess("Success")).ToStatement(),
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						actual.Text,
						SF.EqualsValueClause(
							results.MemberAccess("Data")
						)
					)
				)
			);


			blocks = blocks.AddStatements(generateCompare(var1, actual, props));

			blocks = blocks.AddStatements(
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					results.ToIN(),
					controller.MemberAccess("Get")
					.Invoke(
						SF.Argument(var2.MemberAccess(prop.Name))
					)
				).WithLeadingTrivia(SF.Comment($"// verify {var2}")).ToStatement(),
				TestBuilder.NotNull(results.ToIN()).ToStatement(),
				TestBuilder.True(results.MemberAccess("Success")).ToStatement(),
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						actual.ToIN(),
						results.MemberAccess("Data")
				).ToStatement()
			);

			blocks = blocks.AddStatements(SF.ExpressionStatement(
				TestBuilder.NotNull(Extensions.MemberAccess(actual.Text, prop.Name))
			));
			blocks = blocks.AddStatements(generateCompare(var2, actual, props));

			blocks = blocks.AddStatements(
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					results.ToIN(),
					controller.MemberAccess("Get")
					.Invoke(
						SF.Argument(var3.MemberAccess(prop.Name))
					)
				).WithLeadingTrivia(SF.Comment($"// verify {var3}")).ToStatement(),
				TestBuilder.NotNull(results.ToIN()).ToStatement(),
				TestBuilder.True(results.MemberAccess("Success")).ToStatement(),
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						actual.ToIN(),
						results.MemberAccess("Data")
				).ToStatement()
			);

			blocks = blocks.AddStatements(SF.ExpressionStatement(
				TestBuilder.NotNull(Extensions.MemberAccess(actual.Text, prop.Name))
			));
			blocks = blocks.AddStatements(generateCompare(var3, actual, props));

			/*						results = controller.Get(default(int));
						Assert.NotNull(results);
						Assert.False(results.Success);
						Assert.Equal(1, results.Errors.Count);
						Assert.Equal($"Could not find Device with Id {default(int)}", results.Errors[0]);
*/
			blocks = blocks.AddStatements(
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					results.ToIN(),
					controller.MemberAccess("Get")
					.Invoke(
						SF.Argument(SF.ParseTypeName(prop.PropertyType.Name).GetDefaultValue())
					)
				).WithLeadingTrivia(SF.Comment("// Failed Test")).ToStatement(),
				TestBuilder.NotNull(results.ToIN()).ToStatement(),
				TestBuilder.False(results.MemberAccess("Success")).ToStatement(),
				TestBuilder.Equal(1.ToLiteral(), results.MemberAccess("Errors", "Count")).ToStatement(),
				TestBuilder.Equal(
					$"Could not find {t.Name} with {prop.Name} ".ToInterpolatedString(
						((StringToken)(SF.ParseTypeName(prop.PropertyType.Name).GetDefaultValue().ToString())).AsInterpolation()
					),
					SF.ElementAccessExpression(
						results.MemberAccess("Errors")
					).AddArgumentListArguments(
						SF.Argument(0.ToLiteral())
					)
				).ToStatement()
			);

			method = method.WithBody(wrapBlocks(blocks, controllerName));

			return method;
		}

		private StatementSyntax[] generatePostCommonAssert(SyntaxToken result, String message, int errorCount = 1, bool shouldBeNull=false)
		{
			List<StatementSyntax> statements = new List<StatementSyntax>();
			statements.Add(SF.ExpressionStatement(
					TestBuilder.NotNull(SF.IdentifierName(result))
				));
			statements.Add(
					SF.ExpressionStatement(
						TestBuilder.False(
							Extensions.MemberAccess(
								SF.IdentifierName(result),
								SF.IdentifierName("Success")
							)
						)
					)
				);
			statements.Add(
					SF.ExpressionStatement(
						TestBuilder.Equal(
							errorCount.ToLiteral(),
							result.Text.MemberAccess(
								"Errors",
								"Count"
							)
						)
					)
				);
			statements.Add(
					SF.ExpressionStatement(
						TestBuilder.Equal(
							message.ToLiteral(),
							SF.ElementAccessExpression(
								Extensions.MemberAccess(
									SF.IdentifierName(result),
									SF.IdentifierName("Errors")
								)
							).AddArgumentListArguments(
								SF.Argument(0.ToLiteral())
							)
						)
					)
				);
			if (shouldBeNull)
			{
				statements.Add(
						SF.ExpressionStatement(
							TestBuilder.Null(
								Extensions.MemberAccess(
									SF.IdentifierName(result),
									SF.IdentifierName("Data")
								)
							)
						)
					);
			}
			else
			{
				statements.Add(
						SF.ExpressionStatement(
							TestBuilder.NotNull(
								Extensions.MemberAccess(
									SF.IdentifierName(result),
									SF.IdentifierName("Data")
								)
							)
						)
					);
			}
			return statements.ToArray();
		}

		private MethodDeclarationSyntax generatePostTest(String controllerName, String propertyName, Type t)
		{
			var wrapper = SF.Identifier("wrapper");
			var context = SF.Identifier("context");
			var controller = SF.Identifier("controller");
			var result = SF.Identifier("result");
			var method = SF.MethodDeclaration(SF.ParseTypeName("void"), "PostTest")
				.AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
				.AddAttributeLists(SF.AttributeList().AddAttributes(TestBuilder.GetMethodAttribute()));

			var blocks = SF.Block();
			blocks = blocks.AddStatements(
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						result.Text,
						SF.EqualsValueClause(
							SF.InvocationExpression(
								Extensions.MemberAccess(
									controller.Text,
									"Post"
								)
							).AddArgumentListArguments(
								SF.Argument(SF.LiteralExpression(SyntaxKind.NullLiteralExpression))
							)
						)
					)
				)
			);

			blocks = blocks.AddStatements(
				generatePostCommonAssert(result, "data cannot be null", shouldBeNull: true)
				);

			var expected = "expected";

			blocks = blocks.AddStatements(
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(expected, 
						SF.EqualsValueClause(
							SF.ObjectCreationExpression(SF.ParseTypeName(t.Name))
							.AddArgumentListArguments()
						)
					)
				)
			);

			var props = t.GetProperties();
			var key = props.GetKeyProperty();

			foreach(var prop in props)
			{
				var genArg = prop.GetGenerationAttribute();
				if (genArg != null && !genArg.Ignore)
				{

					if (genArg.CheckForEmptyString)
					{
						blocks = blocks.AddStatements(
							generateSeedObject(expected, t.Name, props, prop.Name)
						);

						blocks = blocks.AddStatements(
							SF.ExpressionStatement(
								SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
									expected.MemberAccess(prop.Name),
									"".ToLiteral()
								)
							),
							SF.ExpressionStatement(
								SF.InvocationExpression(SF.IdentifierName("postPreCall"))
								.AddArgumentListArguments(
									SF.Argument(SF.IdentifierName(expected)),
									SF.Argument(SF.IdentifierName(wrapper))
								)
							),
							SF.ExpressionStatement(
								SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
									SF.IdentifierName(result),
									SF.InvocationExpression(
										SF.IdentifierName(controller).MemberAccess(SF.IdentifierName("Post"))
									).AddArgumentListArguments(
										SF.Argument(SF.IdentifierName(expected))
									)
								)
							)
						);
						blocks = blocks.AddStatements(
							generatePostCommonAssert(result, $"{prop.Name} must have a non empty value")
							);
					}
					if (genArg.IsRequired)
					{
						blocks = blocks.AddStatements(
							generateSeedObject(expected, t.Name, props, prop.Name)
						);

						blocks = blocks.AddStatements(
							SF.ExpressionStatement(
								SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
									expected.MemberAccess(prop.Name),
									SF.LiteralExpression(SyntaxKind.NullLiteralExpression)
								)
							),
							SF.ExpressionStatement(
								SF.InvocationExpression(SF.IdentifierName("postPreCall"))
								.AddArgumentListArguments(
									SF.Argument(SF.IdentifierName(expected)),
									SF.Argument(SF.IdentifierName(wrapper))
								)
							),
							SF.ExpressionStatement(
								SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
									SF.IdentifierName(result),
									SF.InvocationExpression(
										SF.IdentifierName(controller).MemberAccess(SF.IdentifierName("Post"))
									).AddArgumentListArguments(
										SF.Argument(SF.IdentifierName(expected))
									)
								)
							)
						);
						blocks = blocks.AddStatements(
							generatePostCommonAssert(result, $"{prop.Name} must not be null")
							);

					}
				}
			}

			blocks = blocks.AddStatements(
				generateSeedObject(expected, t.Name, props, "Success Test")
			);

			blocks = blocks.AddStatements(
				SF.ExpressionStatement(
					SF.InvocationExpression(SF.IdentifierName("postPreCall"))
					.AddArgumentListArguments(
						SF.Argument(SF.IdentifierName(expected)),
						SF.Argument(SF.IdentifierName(wrapper))
					)
				),
				SF.ExpressionStatement(
					SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						SF.IdentifierName(result),
						SF.InvocationExpression(
							SF.IdentifierName(controller).MemberAccess(SF.IdentifierName("Post"))
						).AddArgumentListArguments(
							SF.Argument(SF.IdentifierName(expected))
						)
					)
				),
				SF.ExpressionStatement(
					TestBuilder.NotNull(SF.IdentifierName(result))
				),
				SF.ExpressionStatement(
					TestBuilder.True(result.Text.MemberAccess("Success"), "Success was not true")
				),
				SF.ExpressionStatement(
					TestBuilder.Equal(
						0.ToLiteral(),
						result.Text.MemberAccess("Errors", "Count")
					)
				),
				SF.ExpressionStatement(
					TestBuilder.NotNull(result.Text.MemberAccess("Data"))
				)
			);

			if (key.PropertyType == typeof(Int32) || key.PropertyType == typeof(Int64) || key.PropertyType == typeof(Int32))
			{
				blocks = blocks.AddStatements(
					SF.ExpressionStatement(
						TestBuilder.True(
							SF.BinaryExpression(SyntaxKind.GreaterThanExpression,
								result.Text.MemberAccess("Data", key.Name),
								0.ToLiteral()
							),
							$"{key.Name} not updated"
						)
					)
				);
			}
			else if (key.PropertyType == typeof(Guid))
			{
				blocks = blocks.AddStatements(
					SF.ExpressionStatement(
						TestBuilder.True(
							SF.BinaryExpression(SyntaxKind.NotEqualsExpression,
								result.Text.MemberAccess("Data", key.Name),
								SF.InvocationExpression(
									"Guid".MemberAccess("NewGuid")
								).AddArgumentListArguments()
							)
						)
					)
				);
			}
			else
			{
				var list = blocks.Statements;
				var existing = list.Last();
				list = list.Replace(
					existing,
					existing.WithTrailingTrivia(SF.Comment($"// the key type {key.PropertyType} is not supported right now"))
					);

				blocks = blocks.WithStatements(
					list
				);
			}

			var resultData = SF.Identifier("resultData");
			var first = SF.Identifier("first");

			blocks = blocks.AddStatements(
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						first.Text,
						SF.EqualsValueClause(
							SF.InvocationExpression(
								context.Text.MemberAccess(propertyName, "FirstOrDefault")
							).AddArgumentListArguments(
								SF.Argument(
									SF.ParenthesizedLambdaExpression(
										SF.BinaryExpression(SyntaxKind.EqualsExpression,
											"i".MemberAccess(key.Name),
											result.Text.MemberAccess("Data", key.Name)
										)
									).AddParameterListParameters(
										SF.Parameter(SF.Identifier("i"))
									)
								)
							)
						)
					)
				),
				SF.ExpressionStatement(
					TestBuilder.NotNull(SF.IdentifierName(first), "first was not set")
				),
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						resultData.Text,
						SF.EqualsValueClause(
							result.Text.MemberAccess("Data")
						)
					)
				)
			);

			foreach(var prop in props)
			{
				if (!prop.ShouldIgnore())
				{
					blocks = blocks.AddStatements(
						SF.ExpressionStatement(
							TestBuilder.Equal(
								first.Text.MemberAccess(prop.Name),
								resultData.Text.MemberAccess(prop.Name)
							)
						)
					);
				}
			}

			method = method.WithBody(wrapBlocks(blocks, controllerName));

			return method;
		}
		private MethodDeclarationSyntax generatePutTest(String controllerName, String propertyName, Type t)
		{
			var wrapper = SF.Identifier("wrapper");
			var context = SF.Identifier("context");
			var controller = SF.Identifier("controller");
			var result = SF.Identifier("result");
			var method = SF.MethodDeclaration(SF.ParseTypeName("void"), "PutTest")
				.AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
				.AddAttributeLists(SF.AttributeList().AddAttributes(TestBuilder.GetMethodAttribute()));

			var blocks = SF.Block();
			blocks = blocks.AddStatements(
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						result.Text,
						SF.EqualsValueClause(
							SF.InvocationExpression(
								Extensions.MemberAccess(
									controller.Text,
									"Put"
								)
							).AddArgumentListArguments(
								SF.Argument(SF.LiteralExpression(SyntaxKind.NullLiteralExpression))
							)
						)
					)
				)
			);

			blocks = blocks.AddStatements(
				generatePostCommonAssert(result, "data cannot be null", shouldBeNull: true)
				);

			var expected = "expected";

			blocks = blocks.AddStatements(
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(expected, 
						SF.EqualsValueClause(
							SF.ObjectCreationExpression(SF.ParseTypeName(t.Name))
							.AddArgumentListArguments()
						)
					)
				)
			);

			var props = t.GetProperties();
			var key = props.GetKeyProperty();

			foreach(var prop in props)
			{
				var genArg = prop.GetGenerationAttribute();
				if (genArg != null && !genArg.Ignore)
				{

					if (genArg.CheckForEmptyString)
					{
						blocks = blocks.AddStatements(
							generateSeedObject(expected, t.Name, props, prop.Name)
						);

						blocks = blocks.AddStatements(
							SF.ExpressionStatement(
								SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
									expected.MemberAccess(prop.Name),
									"".ToLiteral()
								)
							),
							SF.ExpressionStatement(
								SF.InvocationExpression(SF.IdentifierName("putPreCall"))
								.AddArgumentListArguments(
									SF.Argument(SF.IdentifierName(expected)),
									SF.Argument(SF.IdentifierName(wrapper))
								)
							),
							SF.ExpressionStatement(
								SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
									SF.IdentifierName(result),
									SF.InvocationExpression(
										SF.IdentifierName(controller).MemberAccess(SF.IdentifierName("Put"))
									).AddArgumentListArguments(
										SF.Argument(SF.IdentifierName(expected))
									)
								)
							)
						);
						blocks = blocks.AddStatements(
							generatePostCommonAssert(result, $"{prop.Name} must have a non empty value")
							);
					}
					if (genArg.IsRequired)
					{
						blocks = blocks.AddStatements(
							generateSeedObject(expected, t.Name, props, prop.Name)
						);

						blocks = blocks.AddStatements(
							SF.ExpressionStatement(
								SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
									expected.MemberAccess(prop.Name),
									SF.LiteralExpression(SyntaxKind.NullLiteralExpression)
								)
							),
							SF.ExpressionStatement(
								SF.InvocationExpression(SF.IdentifierName("putPreCall"))
								.AddArgumentListArguments(
									SF.Argument(SF.IdentifierName(expected)),
									SF.Argument(SF.IdentifierName(wrapper))
								)
							),
							SF.ExpressionStatement(
								SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
									SF.IdentifierName(result),
									SF.InvocationExpression(
										SF.IdentifierName(controller).MemberAccess(SF.IdentifierName("Put"))
									).AddArgumentListArguments(
										SF.Argument(SF.IdentifierName(expected))
									)
								)
							)
						);
						blocks = blocks.AddStatements(
							generatePostCommonAssert(result, $"{prop.Name} must not be null")
							);

					}
				}
			}

			blocks = blocks.AddStatements(
				generateSeedObject(expected, t.Name, props, "Success Test")
			);

			blocks = blocks.AddStatements(
				SF.ExpressionStatement(
					SF.InvocationExpression(SF.IdentifierName("putPreCall"))
					.AddArgumentListArguments(
						SF.Argument(SF.IdentifierName(expected)),
						SF.Argument(SF.IdentifierName(wrapper))
					)
				),
				SF.ExpressionStatement(
					SF.InvocationExpression(
						context.Text.MemberAccess(propertyName, "Add")
					).AddArgumentListArguments(
						SF.Argument(SF.IdentifierName(expected))
					)
				),
				SF.ExpressionStatement(
					SF.InvocationExpression(
						context.Text.MemberAccess("SaveChanges")
					).AddArgumentListArguments()
				)
			);
			blocks = blocks.AddStatements(
				generateSeedObject(expected, t.Name, props.Where(i => !i.ShouldIgnore() && i.Name != key.Name && !i.CantUpdate()).ToArray(), "Reset props and call put", false)
				);

			blocks = blocks.AddStatements(
				SF.ExpressionStatement(
					SF.InvocationExpression(SF.IdentifierName("putPreCall"))
					.AddArgumentListArguments(
						SF.Argument(SF.IdentifierName(expected)),
						SF.Argument(SF.IdentifierName(wrapper))
					)
				),
				SF.ExpressionStatement(
					SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						SF.IdentifierName(result),
						SF.InvocationExpression(
							SF.IdentifierName(controller).MemberAccess(SF.IdentifierName("Put"))
						).AddArgumentListArguments(
							SF.Argument(SF.IdentifierName(expected))
						)
					)
				),
				SF.ExpressionStatement(
					TestBuilder.NotNull(SF.IdentifierName(result))
				),
				SF.ExpressionStatement(
					TestBuilder.True(result.Text.MemberAccess("Success"), "Success was not true")
				),
				SF.ExpressionStatement(
					TestBuilder.Equal(
						0.ToLiteral(),
						result.Text.MemberAccess("Errors", "Count")
					)
				),
				SF.ExpressionStatement(
					TestBuilder.NotNull(result.Text.MemberAccess("Data"))
				)
			);

			if (key.PropertyType == typeof(Int32) || key.PropertyType == typeof(Int64) || key.PropertyType == typeof(Int32))
			{
				blocks = blocks.AddStatements(
					SF.ExpressionStatement(
						TestBuilder.True(
							SF.BinaryExpression(SyntaxKind.GreaterThanExpression,
								result.Text.MemberAccess("Data", key.Name),
								0.ToLiteral()
							),
							$"{key.Name} not updated"
						)
					)
				);
			}
			else if (key.PropertyType == typeof(Guid))
			{
				blocks = blocks.AddStatements(
					SF.ExpressionStatement(
						TestBuilder.True(
							SF.BinaryExpression(SyntaxKind.NotEqualsExpression,
								result.Text.MemberAccess("Data", key.Name),
								SF.InvocationExpression(
									"Guid".MemberAccess("NewGuid")
								).AddArgumentListArguments()
							)
						)
					)
				);
			}
			else
			{
				var list = blocks.Statements;
				var existing = list.Last();
				list = list.Replace(
					existing,
					existing.WithTrailingTrivia(SF.Comment($"// the key type {key.PropertyType} is not supported right now"))
					);

				blocks = blocks.WithStatements(
					list
				);
			}

			var resultData = SF.Identifier("resultData");
			var first = SF.Identifier("first");

			blocks = blocks.AddStatements(
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						first.Text,
						SF.EqualsValueClause(
							SF.InvocationExpression(
								context.Text.MemberAccess(propertyName, "FirstOrDefault")
							).AddArgumentListArguments(
								SF.Argument(
									SF.ParenthesizedLambdaExpression(
										SF.BinaryExpression(SyntaxKind.EqualsExpression,
											"i".MemberAccess(key.Name),
											result.Text.MemberAccess("Data", key.Name)
										)
									).AddParameterListParameters(
										SF.Parameter(SF.Identifier("i"))
									)
								)
							)
						)
					)
				),
				SF.ExpressionStatement(
					TestBuilder.NotNull(SF.IdentifierName(first), "first was not set")
				),
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(
						resultData.Text,
						SF.EqualsValueClause(
							result.Text.MemberAccess("Data")
						)
					)
				)
			);

			foreach(var prop in props)
			{
				if (!prop.ShouldIgnore())
				{
					blocks = blocks.AddStatements(
						SF.ExpressionStatement(
							TestBuilder.Equal(
								first.Text.MemberAccess(prop.Name),
								resultData.Text.MemberAccess(prop.Name)
							)
						)
					);
				}
			}

			method = method.WithBody(wrapBlocks(blocks, controllerName));

			return method;
		}

		protected override CompilationUnitSyntax internalGenerate(string propertyName, Type t)
		{
			var controllerName = $"{t.Name}Controller";
			var filename = $"{controllerName}Tests";
			var unit = SF.CompilationUnit();

			unit = unit.AddUsing("System").WithLeadingTrivia(GetLicenseComment());
			unit = unit.AddUsings(
					"Sannel.House.Web.Base.Interfaces",
					"Sannel.House.Web.Base.Models",
					"Sannel.House.Web.Controllers.api",
					"Sannel.House.Web.Data",
					"Sannel.House.Web.Mocks",
					"System.Collections.Generic",
					"System.Linq",
					"System.Threading.Tasks",
					"Microsoft.Extensions.Logging");
			unit = unit.AddUsings(TestBuilder.Namespaces);

			var @class = SF.ClassDeclaration(filename)
				.AddModifiers(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.PartialKeyword))
				.AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IContextWrapperTest")));

			var att = TestBuilder.GetClassAttribute();
			if (att != null)
			{
				@class = @class.AddAttributeLists(
					SF.AttributeList().AddAttributes(att)
				);
			}
			var ti = t.GetTypeInfo();
			var ga = ti.GetCustomAttribute<GenerationAttribute>() ?? new GenerationAttribute();

			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Get))
			{
				@class = @class.AddMembers(generateGetTest(controllerName, propertyName, t));
			}
			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.GetWithId))
			{
				@class = @class.AddMembers(generateGetByIdTest(controllerName, propertyName, t));
			}
			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Post))
			{
				@class = @class.AddMembers(generatePostTest(controllerName, propertyName, t));
				@class = @class.AddMembers(SF.MethodDeclaration(SF.ParseTypeName("void"),
					"postPreCall")
					.AddModifiers(SF.Token(SyntaxKind.PartialKeyword))
					.AddParameterListParameters(
						SF.Parameter(SF.Identifier("data")).WithType(SF.ParseTypeName(t.Name)),
						SF.Parameter(SF.Identifier("wrapper")).WithType(SF.ParseTypeName("ContextWrapper"))
					).WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken))
					.WithLeadingTrivia(SF.Comment("// used to make sure reference tables have data needed for a test to succeed"))
				);
			}
			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Put))
			{
				@class = @class.AddMembers(generatePutTest(controllerName, propertyName, t));
				@class = @class.AddMembers(SF.MethodDeclaration(SF.ParseTypeName("void"),
					"putPreCall")
					.AddModifiers(SF.Token(SyntaxKind.PartialKeyword))
					.AddParameterListParameters(
						SF.Parameter(SF.Identifier("data")).WithType(SF.ParseTypeName(t.Name)),
						SF.Parameter(SF.Identifier("wrapper")).WithType(SF.ParseTypeName("ContextWrapper"))
					).WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken))
					.WithLeadingTrivia(SF.Comment("// used to make sure reference tables have data needed for a test to succeed"))
				);
			}

			return unit.AddMembers(SF.NamespaceDeclaration(SF.IdentifierName("Sannel.House.Web.Tests")).AddMembers(@class));
		}
	}
}
