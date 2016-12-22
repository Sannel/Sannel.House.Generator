using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sannel.House.Generator.Common;
using Sannel.House.Web.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Sannel.House.Generator
{
	public static class Extensions
	{
		public static String ReplaceKeys(this String path, PropertyWithName pwn, RunConfig config)
		{
			path = path.Replace("{TypeName}", pwn.Type.Name);
			return path;
		}

		public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
		{
			if(dict == null)
			{
				return default(TValue);
			}
			if (dict.ContainsKey(key))
			{
				return dict[key];
			}

			return default(TValue);
		}

		public static PropertyInfo GetSortProperty(this PropertyInfo[] props, out bool isForward)
		{
			isForward = true;
			if (props == null)
			{
				return null;
			}

			var dm = props.FirstOrDefault(i => String.Compare(i.Name, "DisplayOrder", true) == 0);
			if (dm == null)
			{
				dm = props.FirstOrDefault(i => String.Compare(i.Name, "Order", true) == 0);
			}

			if (dm == null)
			{
				dm = props.FirstOrDefault(i => String.Compare(i.Name, "DateCreated") == 0);
				isForward = false;
			}

			if (dm == null)
			{
				dm = props.FirstOrDefault(i => String.Compare(i.Name, "CreatedDate") == 0);
				isForward = false;
			}

			if (dm == null)
			{
				dm = props.FirstOrDefault(i => String.Compare(i.Name, "CreatedDateTime") == 0);
				isForward = false;
			}

			return dm;
		}

		public static PropertyInfo GetKeyProperty(this PropertyInfo[] props)
		{
			if (props == null)
			{
				return null;
			}

			foreach (var p in props)
			{
				if (p.IsKey())
				{
					return p;
				}
			}

			return null;
		}

		public static bool IsKey(this PropertyInfo info)
		{
			var attr = info.GetCustomAttribute(typeof(KeyAttribute));

			return attr != null;
		}

		public static TypeSyntax GetTypeSyntax(this PropertyInfo info)
		{
			if (info == null)
			{
				throw new ArgumentNullException(nameof(info));
			}

			Type t = info.PropertyType;

			if (t.GenericTypeArguments != null && t.GenericTypeArguments.Length > 0)
			{
				if (String.Compare(t.Name, "Nullable`1") == 0)
				{
					return SF.ParseTypeName($"{t.GenericTypeArguments.First().Name}?");
				}

				throw new Exception($"Type {t.FullName} is not supported right now.");
			}
			else
			{
				return SF.ParseTypeName(t.Name);
			}
		}

		public static CompilationUnitSyntax AddUsing(this CompilationUnitSyntax unit, String namesp)
		{
			return unit.AddUsings(SF.UsingDirective(SF.IdentifierName(namesp)));
		}

		public static CompilationUnitSyntax AddUsings(this CompilationUnitSyntax unit, params String[] usings)
		{
			foreach (var u in usings)
			{
				unit = unit.AddUsings(SF.UsingDirective(SF.IdentifierName(u)));
			}
			return unit;
		}

		public static ArgumentListSyntax AddArgument(this ArgumentListSyntax syntax, String name)
		{
			return syntax.AddArguments(SF.Argument(SF.IdentifierName(name)));
		}

		public static ExpressionSyntax GetRandomValue(this TypeSyntax t, Random rand)
		{
			String type = t.ToString();
			if (t is NullableTypeSyntax)
			{
				var nt = (NullableTypeSyntax)t;
				type = nt.ElementType.ToString();
			}

			switch (type)
			{
				case "Guid":
					return SF.InvocationExpression(
							SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
							SF.IdentifierName("Guid"),
							SF.IdentifierName("NewGuid"))
						).AddArgumentListArguments();

				case "Int16":
				case "Int32":
				case "Int64":
					return rand.Next(1, 200).ToLiteral();

				case "Float":
				case "Double":
				case "Decimal":
					return rand.NextDouble().ToLiteral();

				case "Boolean":
					return (rand.NextDouble() > 0.5).ToLiteral();

				case "String":
					StringBuilder value = new StringBuilder();
					int count = rand.Next(10, 30);
					for(int i = 0; i < count; i++)
					{
						char c = (char)rand.Next((int)'a', (int)'z');
						value.Append(c);
					}
					return value.ToString().ToLiteral();

				case "DateTimeOffset":
					new DateTimeOffset(2000, 2, 3, 3, 12, 13, TimeSpan.FromHours(-6));
					return SF.ObjectCreationExpression(SF.ParseTypeName("DateTimeOffset"))
						.AddArgumentListArguments(
							SF.Argument(rand.Next(1980, 2016).ToLiteral()), // Year
							SF.Argument(rand.Next(1,12).ToLiteral()), // Month
							SF.Argument(rand.Next(1, 28).ToLiteral()), // Day
							SF.Argument(rand.Next(1,24).ToLiteral()), // Hour
							SF.Argument(rand.Next(1, 60).ToLiteral()), // Minutes
							SF.Argument(rand.Next(1, 60).ToLiteral()), // seconds
							SF.Argument(
								SF.InvocationExpression(
									Extensions.MemberAccess(
										SF.IdentifierName("TimeSpan"),
										SF.IdentifierName("FromHours")
									)
								)
								.AddArgumentListArguments(
									SF.Argument(
										rand.Next(-7, -4).ToLiteral()
									)
								)
							) // Offset
						);

				default:
					return $"{type} is not suppored please add support or change its type".ToLiteral();

			}

		}

		public static String GetTypeString(this TypeSyntax t)
		{
			if (t == null)
			{
				return null;
			}
			if (t is NullableTypeSyntax)
			{
				var nt = (NullableTypeSyntax)t;
				return $"{nt.ElementType}?";
			}

			return t.ToString();
		}

		public static ExpressionSyntax GetDefaultValue(this TypeSyntax t)
		{
			String type = t.ToString();
			if (t is NullableTypeSyntax)
			{
				var nt = (NullableTypeSyntax)t;
				type = nt.ElementType.ToString();
			}
			switch (type)
			{
				case "Guid":
					return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					SF.IdentifierName("Guid"),
					SF.IdentifierName("Empty"));

				case "Int32":
				case "Int16":
				case "Int64":
					return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(0));

				case "Float":
				case "Double":
				case "Decimal":
					return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(0));

				case "DayOfWeek":
					return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						SF.IdentifierName("DayOfWeek"),
						SF.IdentifierName("Monday"));

				case "DateTimeOffset":
					return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						SF.IdentifierName("DateTimeOffset"),
						SF.IdentifierName("MinValue"));

				case "String":
					return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						SF.IdentifierName("String"),
						SF.IdentifierName("Empty"));

				case "Boolean":
					return SF.LiteralExpression(SyntaxKind.FalseLiteralExpression);
			}


			return SF.LiteralExpression(SyntaxKind.NullLiteralExpression);
		}

		public static ExpressionSyntax LiteralForProperty(this Random rand, Type t, String name)
		{
			if (t == typeof(bool))
			{
				return (rand.Next(1, 10) >= 5) ? SF.LiteralExpression(SyntaxKind.TrueLiteralExpression) : SF.LiteralExpression(SyntaxKind.FalseLiteralExpression);
			}
			if (t == typeof(String))
			{
				return SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(rand.NextString(10, 50)));
			}
			if (t == typeof(int) || t == typeof(short) || t == typeof(long) || t == typeof(int?))
			{
				return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(rand.Next(1, 100)));
			}
			if (t == typeof(float) || t == typeof(double) || t == typeof(decimal))
			{
				return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(rand.NextDouble()));
			}
			if (t == typeof(Guid))
			{
				return SF.InvocationExpression(
					SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						SF.IdentifierName("Guid"),
						SF.IdentifierName("NewGuid")
					)).AddArgumentListArguments();
			}

			if (t == typeof(DateTime) || t == typeof(DateTime?))
			{
				if (String.Compare(name, "CreatedDate", true) == 0 ||
					String.Compare(name, "CreatedDateTime", true) == 0 ||
					String.Compare(name, "ModifiedDate", true) == 0 ||
					String.Compare(name, "ModifiedDateTime", true) == 0)
				{
					return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						SF.IdentifierName("DateTime"),
						SF.IdentifierName("Now"));
				}
				return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					SF.IdentifierName("DateTime"),
					SF.IdentifierName("Min"));
			}
			if (t == typeof(DateTimeOffset) || t == typeof(DateTimeOffset?))
			{
				if (String.Compare(name, "CreatedDate", true) == 0 ||
					String.Compare(name, "CreatedDateTime", true) == 0 ||
					String.Compare(name, "ModifiedDate", true) == 0 ||
					String.Compare(name, "ModifiedDateTime", true) == 0)
				{
					return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						SF.IdentifierName("DateTimeOffset"),
						SF.IdentifierName("Now"));
				}
				return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					SF.IdentifierName("DateTimeOffset"),
					SF.IdentifierName("Min"));
			}

			if (t == typeof(DayOfWeek?) || t == typeof(DayOfWeek))
			{
				return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					SF.IdentifierName("DayOfWeek"),
					SF.IdentifierName(Enum.GetName(typeof(DayOfWeek), rand.Next(1, 7))));
			}

			throw new Exception($"Unsupported type {t.Name}");

		}

		public static VariableDeclarationSyntax VariableDeclaration(String name, String createType, ArgumentListSyntax list, String declareType = "var")
		{
			return SF.VariableDeclaration(SF.IdentifierName(declareType))
				.AddVariables(SF.VariableDeclarator(name)
					.WithInitializer(SF.EqualsValueClause(
						SF.ObjectCreationExpression(SF.ParseTypeName(createType))
						.WithArgumentList(list)
						)));
		}
		public static VariableDeclarationSyntax VariableDeclaration(String name, EqualsValueClauseSyntax equals, String declareType = "var")
		{
			return SF.VariableDeclaration(SF.IdentifierName(declareType))
				.AddVariables(SF.VariableDeclarator(name)
					.WithInitializer(equals)
				);
		}

		public static VariableDeclaratorSyntax VariableDeclarator(String name, String createType, ArgumentListSyntax list, String declareType = "var")
		{
			return SF.VariableDeclarator(declareType)
					.WithInitializer(SF.EqualsValueClause(
						SF.ObjectCreationExpression(SF.ParseTypeName(createType))
						.WithArgumentList(list)
						));
		}

		public static AssignmentExpressionSyntax SetPropertyValue(IdentifierNameSyntax name, String propertyName, ExpressionSyntax value)
		{
			return SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
				SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
				name,
				SF.IdentifierName(propertyName)),
				value);

		}

		public static MemberAccessExpressionSyntax MemberAccess(String name, String property)
		{
			return MemberAccess(SF.IdentifierName(name), SF.IdentifierName(property));
		}

		public static MemberAccessExpressionSyntax MemberAccess(ExpressionSyntax left, SimpleNameSyntax right)
		{
			return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, left, right);
		}

		public static bool ShouldIgnore(this PropertyInfo pi)
		{
			var att = pi.GetCustomAttribute<GenerationAttribute>();
			return att != null && att.Ignore;
		}

		public static LiteralExpressionSyntax ToLiteral(this int number)
		{
			return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(number));
		}
		public static LiteralExpressionSyntax ToLiteral(this short number)
		{
			return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(number));
		}

		public static LiteralExpressionSyntax ToLiteral(this long number)
		{
			return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(number));
		}
		public static LiteralExpressionSyntax ToLiteral(this float number)
		{
			return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(number));
		}
		public static LiteralExpressionSyntax ToLiteral(this double number)
		{
			return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(number));
		}
		public static LiteralExpressionSyntax ToLiteral(this decimal number)
		{
			return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(number));
		}

		public static LiteralExpressionSyntax ToLiteral(this String value)
		{
			return SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(value));
		}

		public static LiteralExpressionSyntax ToLiteral(this bool value)
		{
			return (value) ? SF.LiteralExpression(SyntaxKind.TrueLiteralExpression) : SF.LiteralExpression(SyntaxKind.FalseLiteralExpression);
		}

		public static StringToken ToStringToken(this SyntaxToken token)
		{
			return ((StringToken)token.Text).AsInterpolation();
		}

		public static InterpolatedStringExpressionSyntax ToInterpolatedString(this String value, params StringToken[] tokens)
		{
			var ise = SF.InterpolatedStringExpression(SF.Token(SyntaxKind.InterpolatedStringStartToken));
			ise = ise.AddContents(
					SF.InterpolatedStringText()
					.WithTextToken(
						SF.Token(
							SF.TriviaList(),
							SyntaxKind.InterpolatedStringTextToken,
							value,
							value,
							SF.TriviaList()
						)
					)
				);

			foreach (var token in tokens)
			{
				if (token.IsInterpolation)
				{
					ise = ise.AddContents(
						SF.Interpolation(SF.IdentifierName(token.Value))
					);
				}
				else
				{
					ise = ise.AddContents(
						SF.InterpolatedStringText()
						.WithTextToken(
							SF.Token(
								SF.TriviaList(),
								SyntaxKind.InterpolatedStringTextToken,
								token.Value,
								token.Value,
								SF.TriviaList()
							)
						)
					);
				}
			}

			return ise;
		}
	}
}
