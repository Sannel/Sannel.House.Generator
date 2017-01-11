using System;
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
using Sannel.House.Web.Base;

namespace Sannel.House.Generator.Generators
{
	public class ControllerGenerator : GeneratorBase
	{
		private SyntaxToken context = SF.Identifier("context");
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
								SF.IdentifierName(context),
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

		private IfStatementSyntax postEmptyStringIfStatment(SyntaxToken data, SyntaxToken result, PropertyInfo info)
		{
				StringToken token = $"nameof({data.Text}.{info.Name})";
				token = token.AsInterpolation();
				var istring = token.ToInterpolatedString(" must have a non empty value");

			return SF.IfStatement(
				SF.InvocationExpression(
					Extensions.MemberAccess(
						SF.IdentifierName("String"),
						SF.IdentifierName("IsNullOrWhiteSpace")
					)
				).AddArgumentListArguments(
					SF.Argument(
						Extensions.MemberAccess(
							SF.IdentifierName(data),
							SF.IdentifierName(info.Name)
						)
					)
				),
				SF.Block().AddStatements(
					SF.ExpressionStatement(
						SF.InvocationExpression(
							Extensions.MemberAccess(
								Extensions.MemberAccess(
									SF.IdentifierName(result),
									SF.IdentifierName("Errors")
								),
								SF.IdentifierName("Add")
							)
						)
						.AddArgumentListArguments(
							SF.Argument(istring)
						)
					),
					SF.ReturnStatement(
						SF.IdentifierName(result)
					)
				)
			);
		}
		private IfStatementSyntax postGreaterThenZeroStatment(SyntaxToken data, SyntaxToken result, PropertyInfo requiredInfo)
		{
			var type = requiredInfo.PropertyType;
				StringToken token = $"nameof({data.Text}.{requiredInfo.Name})";
				token = token.AsInterpolation();
				var istring = token.ToInterpolatedString(" must be greater then 0");

				return SF.IfStatement(
						SF.BinaryExpression(SyntaxKind.LessThanOrEqualExpression,
							Extensions.MemberAccess(
								SF.IdentifierName(data),
								SF.IdentifierName(requiredInfo.Name)
							),
							0.ToLiteral()
						),
						SF.Block().AddStatements(
						SF.ExpressionStatement(
							SF.InvocationExpression(
								Extensions.MemberAccess(
									Extensions.MemberAccess(
										SF.IdentifierName(result),
										SF.IdentifierName("Errors")
									),
									SF.IdentifierName("Add")
								)
							)
							.AddArgumentListArguments(
								SF.Argument(istring)
							)
						),
						SF.ReturnStatement(
							SF.IdentifierName(result)
						)
					)
					);

		}

		private IfStatementSyntax postRequiredIfStatment(SyntaxToken data, SyntaxToken result, PropertyInfo requiredInfo)
		{
			var type = requiredInfo.PropertyType;
				StringToken token = $"nameof({data.Text}.{requiredInfo.Name})";
				token = token.AsInterpolation();
				var istring = token.ToInterpolatedString(" must not be null");

				return SF.IfStatement(
						SF.BinaryExpression(SyntaxKind.EqualsExpression,
							Extensions.MemberAccess(
								SF.IdentifierName(data),
								SF.IdentifierName(requiredInfo.Name)
							),
							SF.LiteralExpression(SyntaxKind.NullLiteralExpression)
						),
						SF.Block().AddStatements(
						SF.ExpressionStatement(
							SF.InvocationExpression(
								Extensions.MemberAccess(
									Extensions.MemberAccess(
										SF.IdentifierName(result),
										SF.IdentifierName("Errors")
									),
									SF.IdentifierName("Add")
								)
							)
							.AddArgumentListArguments(
								SF.Argument(istring)
							)
						),
						SF.ReturnStatement(
							SF.IdentifierName(result)
						)
					)
					);
		}

		private MethodDeclarationSyntax generatePostMethod(String propertyName, Type t)
		{
			var props = t.GetProperties();
			var data = SF.Identifier("data");
			var result = SF.Identifier("result");

			var key = props.GetKeyProperty();
			if (key == null)
			{
				return null;
			}

			var method = SF.MethodDeclaration(SF.GenericName("Result")
					.AddTypeArgumentListArguments(SF.ParseTypeName(t.Name))
					, "Post")
				.AddModifiers(SF.Token(SyntaxKind.PublicKeyword))
				.AddParameterListParameters(
					SF.Parameter(data)
						.WithType(SF.ParseTypeName(t.Name))
						.AddAttributeLists(
							SF.AttributeList()
							.AddAttributes(SF.Attribute(SF.IdentifierName("FromBody")))
						)
				)
				.WithAttributeLists(
					new SyntaxList<AttributeListSyntax>().Add(
						SF.AttributeList().AddAttributes(
							SF.Attribute(SF.IdentifierName("HttpPost"))
						)
					)
				);

			var blocks = SF.Block();
			/*var result = new Result<Device>();
			result.Data = device;
			result.Success = false;*/
			blocks = blocks.AddStatements(
				SF.LocalDeclarationStatement(
					Extensions.VariableDeclaration(result.Text,
						SF.EqualsValueClause(
							SF.ObjectCreationExpression(SF.GenericName("Result").AddTypeArgumentListArguments(
									SF.ParseTypeName(t.Name)
							)).AddArgumentListArguments()
						)
					)
				),
				SF.ExpressionStatement(
					SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						Extensions.MemberAccess(
							result.Text,
							"Data"
						),
						SF.IdentifierName(data)
					)
				),
				SF.ExpressionStatement(
					SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						Extensions.MemberAccess(
							result.Text,
							"Success"
						),
						false.ToLiteral()
					)
				)
				);

			/*if (device == null)
			{
				result.Errors.Add($"{nameof(device)} cannot be null");
				return result;
			}*/
			blocks = blocks.AddStatements(
				SF.IfStatement(
	SF.BinaryExpression(
		SyntaxKind.EqualsExpression,
		SF.IdentifierName("data"),
		SF.LiteralExpression(
			SyntaxKind.NullLiteralExpression)),
   SF.Block(
		SF.ExpressionStatement(
			SF.InvocationExpression(
				SF.MemberAccessExpression(
					SyntaxKind.SimpleMemberAccessExpression,
					SF.MemberAccessExpression(
						SyntaxKind.SimpleMemberAccessExpression,
						SF.IdentifierName("result"),
						SF.IdentifierName("Errors")),
					SF.IdentifierName("Add")))
			.WithArgumentList(
				SF.ArgumentList(
					SF.SingletonSeparatedList<ArgumentSyntax>(
						SF.Argument(
							SF.InterpolatedStringExpression(
								SF.Token(SyntaxKind.InterpolatedStringStartToken))
							.WithContents(
								SF.List<InterpolatedStringContentSyntax>(
									new InterpolatedStringContentSyntax[]{
										SF.Interpolation(
											SF.InvocationExpression(
												SF.IdentifierName("nameof"))
											.WithArgumentList(
												SF.ArgumentList(
													SF.SingletonSeparatedList<ArgumentSyntax>(
													   SF.Argument(
															SF.IdentifierName("data")))))),
										SF.InterpolatedStringText()
										.WithTextToken(
											SF.Token(
												SF.TriviaList(),
												SyntaxKind.InterpolatedStringTextToken,
												" cannot be null",
												" cannot be null",
												SF.TriviaList()))}))))))),
		SF.ReturnStatement(
			SF.IdentifierName("result"))))
				);

			var keyType = SF.ParseTypeName(key.PropertyType.Name);
			ExpressionSyntax defaultValue = keyType.GetDefaultValue();
			if(key.PropertyType == typeof(Guid))
			{
				Random rand = new Random();
				defaultValue = rand.LiteralForProperty(key.PropertyType, key.Name);
			}

			blocks = blocks.AddStatements(
				SF.ExpressionStatement(
				SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
					Extensions.MemberAccess(
						SF.IdentifierName(data),
						SF.IdentifierName(key.Name)
					),
					defaultValue
				)
				)
				);

			foreach(var prop in props)
			{
				if (!prop.ShouldIgnore() && !prop.IsKey())
				{
					var genAtt = prop.GetGenerationAttribute();
					if (genAtt != null)
					{
						if (genAtt.IsRequired)
						{
							var @if = postRequiredIfStatment(data, result, prop);
							if (@if != null)
							{
								blocks = blocks.AddStatements(@if);
							}
						}
						if (genAtt.CheckForEmptyString)
						{
							var @if = postEmptyStringIfStatment(data, result, prop);
							if (@if != null)
							{
								blocks = blocks.AddStatements(@if);
							}
						}
						if (genAtt.GreaterThenZero)
						{
							var @if = postGreaterThenZeroStatment(data, result, prop);
							if(@if != null)
							{
								blocks = blocks.AddStatements(@if);
							}
						}
					}
				}
			}

			blocks = blocks.AddStatements(
				SF.ExpressionStatement(
					SF.InvocationExpression(
						SF.IdentifierName("postExtraVerification")
					).AddArgumentListArguments(
						SF.Argument(SF.IdentifierName(data)),
						SF.Argument(SF.IdentifierName(result))
					)
				),
				SF.IfStatement(
					SF.BinaryExpression(SyntaxKind.GreaterThanExpression,
						Extensions.MemberAccess(
							Extensions.MemberAccess(
								SF.IdentifierName(result),
								SF.IdentifierName("Errors")
							),
							SF.IdentifierName("Count")
						)
						,
						0.ToLiteral()
					),
					SF.Block().AddStatements(
						SF.ReturnStatement(
							SF.IdentifierName(result)
						)
					)
				)
				);

			foreach(var prop in props)
			{
				if (!prop.ShouldIgnore() && !prop.IsKey())
				{
					var genAtt = prop.GetGenerationAttribute();
					if (genAtt != null)
					{
						if (genAtt.IsNow)
						{
							if(prop.PropertyType == typeof(DateTime))
							{
								blocks = blocks.AddStatements(
									SF.ExpressionStatement(
										SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
											Extensions.MemberAccess(
												SF.IdentifierName(data),
												SF.IdentifierName(prop.Name)
											),
											Extensions.MemberAccess(
												SF.IdentifierName("DateTime"),
												SF.IdentifierName("Now")
											)
										)
									)
								);
							}
							if(prop.PropertyType == typeof(DateTimeOffset))
							{
								blocks = blocks.AddStatements(
									SF.ExpressionStatement(
										SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
											Extensions.MemberAccess(
												SF.IdentifierName(data),
												SF.IdentifierName(prop.Name)
											),
											Extensions.MemberAccess(
												SF.IdentifierName("DateTimeOffset"),
												SF.IdentifierName("Now")
											)
										)
									)
								);
							}
						}
						if (genAtt.ShouldBeCount)
						{
							blocks = blocks.AddStatements(
								SF.ExpressionStatement(
									SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
										Extensions.MemberAccess(
											SF.IdentifierName(data),
											SF.IdentifierName(prop.Name)
										),
										SF.InvocationExpression(
											Extensions.MemberAccess(
												Extensions.MemberAccess(
													SF.IdentifierName(context),
													SF.IdentifierName(propertyName)
												),
												SF.IdentifierName("Count")
											)
										).AddArgumentListArguments()
									)
								)
							);
						}
					}
				}
			}

			blocks = blocks.AddStatements(
				SF.ExpressionStatement(
					SF.InvocationExpression(
						SF.IdentifierName("postExtraReset")
					).AddArgumentListArguments(
						SF.Argument(SF.IdentifierName(data))
					)
				),
				SF.ExpressionStatement(
					SF.InvocationExpression(
						Extensions.MemberAccess(
							Extensions.MemberAccess(
								SF.IdentifierName(context),
								SF.IdentifierName(propertyName)
							),
							SF.IdentifierName("Add")
						)
					).AddArgumentListArguments(
						SF.Argument(SF.IdentifierName(data))
					)
				)
			);

			var ex = SF.Identifier("ex");

			blocks = blocks.AddStatements(
				SF.TryStatement()
				.AddBlockStatements(
					SF.ExpressionStatement(
						SF.InvocationExpression(
							Extensions.MemberAccess(
								SF.IdentifierName(context),
								SF.IdentifierName("SaveChanges")
							)
						).AddArgumentListArguments()
					)
				)
				.AddCatches(
					SF.CatchClause()
					.WithDeclaration(
						SF.CatchDeclaration(
							SF.ParseTypeName("Exception"),
							ex
						)
					).AddBlockStatements(
						SF.ExpressionStatement(
							SF.InvocationExpression(
								Extensions.MemberAccess(
									Extensions.MemberAccess(
										SF.IdentifierName(result),
										SF.IdentifierName("Errors")
									),
									SF.IdentifierName("Add")
								)
							).AddArgumentListArguments(
								SF.Argument(
									Extensions.MemberAccess(
										SF.IdentifierName(ex),
										SF.IdentifierName("Message")
									)
								)
							)
						),
						SF.ReturnStatement(
							SF.IdentifierName(result)
						)
					)
				)
			);

			blocks = blocks.AddStatements(
				SF.ExpressionStatement(
					SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						Extensions.MemberAccess(
							SF.IdentifierName(result),
							SF.IdentifierName("Success")
						),
						true.ToLiteral()
					)
				),
				SF.ReturnStatement(
					SF.IdentifierName(result)
				)
			);

			method = method.WithBody(blocks);

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

			var ti = t.GetTypeInfo();
			var ga = ti.GetCustomAttribute<GenerationAttribute>() ?? new GenerationAttribute();

			var @class = SF.ClassDeclaration(fileName).AddModifiers(SF.Token(SyntaxKind.PublicKeyword), SF.Token(SyntaxKind.PartialKeyword)).AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("Controller")));

			@class = @class.AddAttributeLists(SF.AttributeList().AddAttributes(SF.Attribute(
					SF.IdentifierName("Route")
				).AddArgumentListArguments(
					SF.AttributeArgument(
						SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal("api/[controller]"))
					)
				)));


			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Get))
			{
				@class = @class.AddMembers(generateGetMethod(propertyName, t));
			}

			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.GetWithId))
			{
				var get2 = generateGetWithIdMethod(propertyName, t);
				if (get2 != null)
				{
					@class = @class.AddMembers(get2);
				}
			}

			if (ga.ShouldGenerateMethod(GenerationAttribute.ApiCalls.Post))
			{
				var post = generatePostMethod(propertyName, t);
				if (post != null)
				{
					@class = @class.AddMembers(post);
					/*	partial void postExtraVerification(Device device, Result<Device> result);
			partial void postExtraReset(Device device, Result<Device> result);*/
					@class = @class.AddMembers(SF.MethodDeclaration(SF.ParseTypeName("void"),
						"postExtraVerification")
						.AddModifiers(SF.Token(SyntaxKind.PartialKeyword))
						.AddParameterListParameters(
							SF.Parameter(SF.Identifier("data")).WithType(SF.ParseTypeName(t.Name)),
							SF.Parameter(SF.Identifier("result")).WithType(
								SF.GenericName("Result")
								.AddTypeArgumentListArguments(
									SF.ParseTypeName(t.Name)
								)
							)
						).WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken))
					);
					@class = @class.AddMembers(SF.MethodDeclaration(SF.ParseTypeName("void"),
						"postExtraReset")
						.AddModifiers(SF.Token(SyntaxKind.PartialKeyword))
						.AddParameterListParameters(
							SF.Parameter(SF.Identifier("data")).WithType(SF.ParseTypeName(t.Name))
						).WithSemicolonToken(SF.Token(SyntaxKind.SemicolonToken))
					);
				}
			}


			var namesp = SF.NamespaceDeclaration(SF.ParseName("Sannel.House.Web.Controllers.api"));
			namesp = namesp.AddMembers(@class);
			var syntax = unit.AddMembers(namesp);
			return syntax;
		}
	}
}
