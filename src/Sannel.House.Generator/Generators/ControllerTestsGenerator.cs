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
							wrapper.Text,
							"SaveChanges"
						)
					)
				)
					);

			return blocks;
		}

		private StatementSyntax[] generateSeedObject(String variableName, String typeName, PropertyInfo[] props, String ignorePropertyName)
		{
			var rand = new Random();
			List<ExpressionStatementSyntax> statements = new List<ExpressionStatementSyntax>();

			statements.Add(
				SF.ExpressionStatement(
					SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						SF.IdentifierName(variableName),
						SF.ObjectCreationExpression(SF.ParseTypeName(typeName))
						.AddArgumentListArguments()
					)
				).WithLeadingTrivia(SF.Comment($"// {ignorePropertyName} test"))
				);

			foreach (var p in props)
			{
				if (!p.ShouldIgnore() && String.Compare(p.Name, ignorePropertyName, true) != 0)
				{
					statements.Add(SF.ExpressionStatement(Extensions.SetPropertyValue(SF.IdentifierName(variableName), p.Name, rand.LiteralForProperty(p.PropertyType, p.Name))));
				}
			}

			return statements.ToArray();
		}

		private MethodDeclarationSyntax generateGetTest(String controllerName, String propertyName, Type t)
		{
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


			method = method.AddBodyStatements(wrapBlocks(blocks, controllerName));

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

			blocks = blocks.AddStatements(SF.ExpressionStatement(
				TestBuilder.AssertIsNotNull(Extensions.MemberAccess(actual.Text, prop.Name))
			));
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

			blocks = blocks.AddStatements(SF.ExpressionStatement(
				TestBuilder.AssertIsNotNull(Extensions.MemberAccess(actual.Text, prop.Name))
			));
			blocks = blocks.AddStatements(generateCompare(var3, actual, props));

			method = method.AddBodyStatements(wrapBlocks(blocks, controllerName));

			return method;
		}

		private StatementSyntax[] generatePostCommonAssert(SyntaxToken result, String message, int errorCount = 1, bool shouldBeNull=false)
		{
			List<StatementSyntax> statements = new List<StatementSyntax>();
			statements.Add(SF.ExpressionStatement(
					TestBuilder.AssertIsNotNull(SF.IdentifierName(result))
				));
			statements.Add(
					SF.ExpressionStatement(
						TestBuilder.AssertIsFalse(
							Extensions.MemberAccess(
								SF.IdentifierName(result),
								SF.IdentifierName("Success")
							)
						)
					)
				);
			statements.Add(
					SF.ExpressionStatement(
						TestBuilder.AssertAreEqual(
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
						TestBuilder.AssertAreEqual(
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
							TestBuilder.AssertIsNull(
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
							TestBuilder.AssertIsNotNull(
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
					TestBuilder.AssertIsNotNull(SF.IdentifierName(result))
				),
				SF.ExpressionStatement(
					TestBuilder.AssertIsTrue(result.Text.MemberAccess("Success"), "Success was not true")
				),
				SF.ExpressionStatement(
					TestBuilder.AssertAreEqual(
						0.ToLiteral(),
						result.Text.MemberAccess("Errors", "Count")
					)
				),
				SF.ExpressionStatement(
					TestBuilder.AssertIsNotNull(result.Text.MemberAccess("Data"))
				)
			);

			if (key.PropertyType == typeof(Int32) || key.PropertyType == typeof(Int64) || key.PropertyType == typeof(Int32))
			{
				blocks = blocks.AddStatements(
					SF.ExpressionStatement(
						TestBuilder.AssertIsTrue(
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
						TestBuilder.AssertIsTrue(
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
					TestBuilder.AssertIsNotNull(SF.IdentifierName(first), "first was not set")
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
							TestBuilder.AssertAreEqual(
								first.Text.MemberAccess(prop.Name),
								resultData.Text.MemberAccess(prop.Name)
							)
						)
					);
				}
			}

			method = method.AddBodyStatements(wrapBlocks(blocks, controllerName));

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

			return unit.AddMembers(SF.NamespaceDeclaration(SF.IdentifierName("Sannel.House.Web.Tests")).AddMembers(@class));
		}
	}
}
