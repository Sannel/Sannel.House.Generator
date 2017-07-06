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
		public static StatementSyntax GenerateRandomObject(this Type t, string variableName, Random rand, ExpressionSyntax keyValue=null, bool shouldDeclare=true)
		{
			var pi = t.GetProperties();
			var key = pi.GetKeyProperty();
			var keyST = key.GetTypeSyntax();

			var list = SF.SeparatedList<ExpressionSyntax>().Add(
					SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						SF.IdentifierName(key.Name),
						keyValue ?? keyST.GetRandomValue(rand, key.PropertyType)
					)
				);

			foreach(var p in pi)
			{
				if (!p.ShouldIgnore() && !p.IsKey())
				{
					var st = p.GetTypeSyntax();
					list = list.Add(
						SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
							SF.IdentifierName(p.Name),
							st.GetRandomValue(rand, p.PropertyType)
						)
					);
				}
			}

			if (shouldDeclare)
			{
				return SF.LocalDeclarationStatement(
					VariableDeclaration(variableName,
						SF.EqualsValueClause(
							SF.ObjectCreationExpression(SF.ParseTypeName(t.Name))
							.AddArgumentListArguments()
							.WithInitializer(
								SF.InitializerExpression(SyntaxKind.ObjectInitializerExpression, list)
							)
						)
					)
				);
			}
			else
			{
				return SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
						variableName.ToIN(),
						SF.ObjectCreationExpression(SF.ParseTypeName(t.Name))
						.AddArgumentListArguments()
						.WithInitializer(
							SF.InitializerExpression(SyntaxKind.ObjectInitializerExpression, list)
						)
					).ToStatement();
			}
		}

		public static ExpressionSyntax ToIN(this string identifier)
		{
			return SF.IdentifierName(identifier);
		}

		public static ElementAccessExpressionSyntax ElementAccess(this string name, int index)
		{
			return SF.ElementAccessExpression(SF.IdentifierName(name)).AddArgumentListArguments(SF.Argument(index.ToLiteral()));
		}

		public static ElementAccessExpressionSyntax ElementAccess(this string name, string index)
		{
			return SF.ElementAccessExpression(SF.IdentifierName(name)).AddArgumentListArguments(SF.Argument(index.ToLiteral()));
		}

		public static ElementAccessExpressionSyntax ElementAccess(this MemberAccessExpressionSyntax syntax, int index)
		{
			return SF.ElementAccessExpression(syntax).AddArgumentListArguments(SF.Argument(index.ToLiteral()));
		}
		public static ElementAccessExpressionSyntax ElementAccess(this MemberAccessExpressionSyntax syntax, string index)
		{
			return SF.ElementAccessExpression(syntax).AddArgumentListArguments(SF.Argument(index.ToLiteral()));
		}

		public static string ReplaceKeys(this string path, PropertyWithName pwn, RunConfig config)
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

			var dm = props.FirstOrDefault(i => string.Compare(i.Name, "DisplayOrder", true) == 0);
			if (dm == null)
			{
				dm = props.FirstOrDefault(i => string.Compare(i.Name, "Order", true) == 0);
			}

			if (dm == null)
			{
				dm = props.FirstOrDefault(i => string.Compare(i.Name, "DateCreated") == 0);
				isForward = false;
			}

			if (dm == null)
			{
				dm = props.FirstOrDefault(i => string.Compare(i.Name, "CreatedDate") == 0);
				isForward = false;
			}

			if (dm == null)
			{
				dm = props.FirstOrDefault(i => string.Compare(i.Name, "CreatedDateTime") == 0);
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

			var t = info.PropertyType;

			return t.GetTypeSyntax();
		}

		public static TypeSyntax GetTypeSyntax(this Type t)
		{
			if (t.GenericTypeArguments != null && t.GenericTypeArguments.Length > 0)
			{
				if (string.Compare(t.Name, "Nullable`1") == 0)
				{
					var first = t.GenericTypeArguments.First();
					if (typeof(Enum).IsAssignableFrom(first))
					{
						return SF.ParseTypeName($"{first.FullName}?");
					}
					else
					{
						return SF.ParseTypeName($"{first.Name}?");
					}
				}

				throw new Exception($"Type {t.FullName} is not supported right now.");
			}
			else if (typeof(Enum).IsAssignableFrom(t))
			{
				return SF.ParseTypeName(t.FullName);
			}
			else
			{
				return SF.ParseTypeName(t.Name);
			}
		}

		public static CompilationUnitSyntax AddUsing(this CompilationUnitSyntax unit, string namesp)
		{
			return unit.AddUsings(SF.UsingDirective(SF.IdentifierName(namesp)));
		}

		public static CompilationUnitSyntax AddUsings(this CompilationUnitSyntax unit, params string[] usings)
		{
			foreach (var u in usings)
			{
				unit = unit.AddUsings(SF.UsingDirective(SF.IdentifierName(u)));
			}
			return unit;
		}

		public static ArgumentListSyntax AddArgument(this ArgumentListSyntax syntax, string name)
		{
			return syntax.AddArguments(SF.Argument(SF.IdentifierName(name)));
		}

		public static ExpressionSyntax GetRandomValue(this TypeSyntax t, Random rand, Type actualType)
		{
			var type = t.ToString();
			if (t is NullableTypeSyntax nt)
			{
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
					var value = new StringBuilder();
					var count = rand.Next(10, 30);
					for(var i = 0; i < count; i++)
					{
						var c = (char)rand.Next('a', 'z');
						value.Append(c);
					}
					return value.ToString().ToLiteral();

				case "DateTime":
					return SF.ObjectCreationExpression(SF.ParseTypeName("DateTime"))
						.AddArgumentListArguments(
							SF.Argument(rand.Next(1980, 2016).ToLiteral()), // Year
							SF.Argument(rand.Next(1,12).ToLiteral()), // Month
							SF.Argument(rand.Next(1, 28).ToLiteral()), // Day
							SF.Argument(rand.Next(1,24).ToLiteral()), // Hour
							SF.Argument(rand.Next(1, 60).ToLiteral()), // Minutes
							SF.Argument(rand.Next(1, 60).ToLiteral()) // seconds
						);

				case "DateTimeOffset":
					//new DateTimeOffset(2000, 2, 3, 3, 12, 13, TimeSpan.FromHours(-6));
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
					if (typeof(Enum).IsAssignableFrom(actualType))
					{
						var values = Enum.GetNames(actualType);
						var selectedValue = rand.Next(0, values.Length - 1);

						return actualType.FullName.MemberAccess(values[selectedValue]);
					}
					return $"{type} is not suppored please add support or change its type".ToLiteral();

			}

		}

		public static string GetTypeString(this TypeSyntax t)
		{
			if (t == null)
			{
				return null;
			}
			if (t is NullableTypeSyntax nt)
			{
				return $"{nt.ElementType}?";
			}

			return t.ToString();
		}

		public static ExpressionSyntax GetDefaultValue(this TypeSyntax t)
		{
			var type = t.ToString();
			if (t is NullableTypeSyntax nt)
			{
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

				case "DateTime":
					return "DateTime".MemberAccess("MinValue");

				case "String":
					return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						SF.IdentifierName("String"),
						SF.IdentifierName("Empty"));

				case "Boolean":
					return SF.LiteralExpression(SyntaxKind.FalseLiteralExpression);
			}


			return SF.InvocationExpression(SF.IdentifierName("default"))
				.AddArgumentListArguments(
					SF.Argument(SF.IdentifierName(type))
				);
		}

		public static ExpressionSyntax LiteralForObject(this object obj)
		{
			if(obj != null)
			{
				var type = obj.GetType();
				if(type == typeof(bool))
				{
					return ((bool)obj).ToLiteral();
				}
				if(type == typeof(int))
				{
					return ((int)obj).ToLiteral();
				}
				if(type == typeof(double))
				{
					return ((double)obj).ToLiteral();
				}
				if(type == typeof(string))
				{
					return ((string)obj).ToLiteral();
				}

			}
			return SF.LiteralExpression(SyntaxKind.NullLiteralExpression);
		}

		public static ExpressionSyntax LiteralForProperty(this Random rand, Type t, string name)
		{
			if (t == typeof(bool))
			{
				return (rand.Next(1, 10) >= 5) ? SF.LiteralExpression(SyntaxKind.TrueLiteralExpression) : SF.LiteralExpression(SyntaxKind.FalseLiteralExpression);
			}
			if (t == typeof(string))
			{
				return SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(rand.NextString(10, 50)));
			}
			if (t == typeof(int) || t == typeof(short) || t == typeof(long) || t == typeof(int?))
			{
				return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(rand.Next(1, 100)));
			}
			if (t == typeof(float) || t == typeof(double) || t == typeof(double?) || t == typeof(decimal))
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
				if (string.Compare(name, "CreatedDate", true) == 0 ||
					string.Compare(name, "CreatedDateTime", true) == 0 ||
					string.Compare(name, "ModifiedDate", true) == 0 ||
					string.Compare(name, "ModifiedDateTime", true) == 0)
				{
					return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						SF.IdentifierName("DateTime"),
						SF.IdentifierName("Now"));
				}
				return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					SF.IdentifierName("DateTime"),
					SF.IdentifierName("MinValue"));
			}
			if (t == typeof(DateTimeOffset) || t == typeof(DateTimeOffset?))
			{
				if (string.Compare(name, "CreatedDate", true) == 0 ||
					string.Compare(name, "CreatedDateTime", true) == 0 ||
					string.Compare(name, "ModifiedDate", true) == 0 ||
					string.Compare(name, "ModifiedDateTime", true) == 0)
				{
					return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
						SF.IdentifierName("DateTimeOffset"),
						SF.IdentifierName("Now"));
				}
				return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					SF.IdentifierName("DateTimeOffset"),
					SF.IdentifierName("MinValue"));
			}

			if (t == typeof(DayOfWeek?) || t == typeof(DayOfWeek))
			{
				return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
					SF.IdentifierName("DayOfWeek"),
					SF.IdentifierName(Enum.GetName(typeof(DayOfWeek), rand.Next(1, 7))));
			}

			if (typeof(Enum).IsAssignableFrom(t))
			{
				var values = Enum.GetNames(t);
				var selectedValue = rand.Next(0, values.Length - 1);

				return t.FullName.MemberAccess(values[selectedValue]);
			}

			throw new Exception($"Unsupported type {t.Name}");

		}

		public static VariableDeclarationSyntax VariableDeclaration(string name, string createType, ArgumentListSyntax list, string declareType = "var")
		{
			return SF.VariableDeclaration(SF.IdentifierName(declareType))
				.AddVariables(SF.VariableDeclarator(name)
					.WithInitializer(SF.EqualsValueClause(
						SF.ObjectCreationExpression(SF.ParseTypeName(createType))
						.WithArgumentList(list)
						)));
		}
		public static VariableDeclarationSyntax VariableDeclaration(string name, EqualsValueClauseSyntax equals, string declareType = "var")
		{
			return SF.VariableDeclaration(SF.IdentifierName(declareType))
				.AddVariables(SF.VariableDeclarator(name)
					.WithInitializer(equals)
				);
		}

		public static VariableDeclaratorSyntax VariableDeclarator(string name, string createType, ArgumentListSyntax list, string declareType = "var")
		{
			return SF.VariableDeclarator(declareType)
					.WithInitializer(SF.EqualsValueClause(
						SF.ObjectCreationExpression(SF.ParseTypeName(createType))
						.WithArgumentList(list)
						));
		}

		public static AssignmentExpressionSyntax SetPropertyValue(IdentifierNameSyntax name, string propertyName, ExpressionSyntax value)
		{
			return SF.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
				SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
				name,
				SF.IdentifierName(propertyName)),
				value);

		}

		/// <summary>
		/// Converts syntaxToken to IdentifierName
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public static SimpleNameSyntax ToIN(this SyntaxToken token)
		{
			return SF.IdentifierName(token);
		}

		public static ExpressionStatementSyntax ToStatement(this ExpressionSyntax es)
		{
			return SF.ExpressionStatement(es);
		}

		public static InvocationExpressionSyntax Invoke(this MemberAccessExpressionSyntax memberAccess, params ArgumentSyntax[] arguments)
		{
			return SF.InvocationExpression(memberAccess)
				.AddArgumentListArguments(arguments);
		}

		public static MemberAccessExpressionSyntax MemberAccess(string name, string property)
		{
			return MemberAccess(SF.IdentifierName(name), SF.IdentifierName(property));
		}

		public static MemberAccessExpressionSyntax MemberAccess(this string left, string right, params string[] moreRight)
		{
			return SF.IdentifierName(left).MemberAccess(SF.IdentifierName(right),
				moreRight?.Select(i => SF.IdentifierName(i))?.ToArray()
				);
		}
		
		public static MemberAccessExpressionSyntax MemberAccess(this ExpressionSyntax left, SimpleNameSyntax right, params SimpleNameSyntax[] moreRight)
		{
			var root = MemberAccess(left, right);
			if(moreRight != null)
			{
				foreach(var item in moreRight)
				{
					root = MemberAccess(root, item);
				}
			}

			return root;
		}

		public static MemberAccessExpressionSyntax MemberAccess(this ExpressionSyntax left, SimpleNameSyntax right)
		{
			return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, left, right);
		}

		public static MemberAccessExpressionSyntax MemberAccess(this ExpressionSyntax left, string right)
		{
			return SF.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, left, SF.IdentifierName(right));
		}

		public static MemberAccessExpressionSyntax MemberAccess(this ExpressionSyntax left, string right, params string[] extra)
		{
			return MemberAccess(left, SF.IdentifierName(right), extra?.Select(i => SF.IdentifierName(i))?.ToArray());
		}

		public static MemberAccessExpressionSyntax MemberAccess(this SyntaxToken left, string right, params string[] extra)
		{
			return MemberAccess(SF.IdentifierName(left), SF.IdentifierName(right), extra?.Select(i => SF.IdentifierName(i))?.ToArray());
		}

		public static bool ShouldIgnore(this PropertyInfo pi)
		{
			var att = pi.GetCustomAttribute<GenerationAttribute>();
			return att != null && att.Ignore;
		}

		public static bool HasAlwaysValue(this PropertyInfo pi)
		{
			var att = pi.GetCustomAttribute<GenerationAttribute>();
			return att != null && att.AlwaysValue != null;
		}

		public static object GetAlwaysValue(this PropertyInfo pi)
		{
			var att = pi.GetCustomAttribute<GenerationAttribute>();
			if(att != null)
			{
				return att.AlwaysValue;
			}
			return null;
		}

		public static GenerationAttribute GetGenerationAttribute(this PropertyInfo pi)
		{
			return pi.GetCustomAttribute<GenerationAttribute>();
		}

		public static bool IsRequired(this PropertyInfo pi)
		{
			var att = pi.GetCustomAttribute<GenerationAttribute>();
			return att != null && att.IsRequired;
		}

		public static bool CantUpdate(this PropertyInfo pi)
		{
			var att = pi.GetCustomAttribute<GenerationAttribute>();
			return att != null && att.CantUpdate;
		}

		public static ArgumentSyntax ToArgument(this int number)
		{
			return SF.Argument(number.ToLiteral());
		}

		public static LiteralExpressionSyntax ToLiteral(this int number)
		{
			return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(number));
		}
		public static ArgumentSyntax ToArgument(this short number)
		{
			return SF.Argument(number.ToLiteral());
		}

		public static LiteralExpressionSyntax ToLiteral(this short number)
		{
			return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(number));
		}

		public static ArgumentSyntax ToArgument(this long number)
		{
			return SF.Argument(number.ToLiteral());
		}

		public static LiteralExpressionSyntax ToLiteral(this long number)
		{
			return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(number));
		}
		public static ArgumentSyntax ToArgument(this float number)
		{
			return SF.Argument(number.ToLiteral());
		}

		public static LiteralExpressionSyntax ToLiteral(this float number)
		{
			return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(number));
		}
		public static ArgumentSyntax ToArgument(this double number)
		{
			return SF.Argument(number.ToLiteral());
		}

		public static LiteralExpressionSyntax ToLiteral(this double number)
		{
			return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(number));
		}
		public static ArgumentSyntax ToArgument(this decimal number)
		{
			return SF.Argument(number.ToLiteral());
		}

		public static LiteralExpressionSyntax ToLiteral(this decimal number)
		{
			return SF.LiteralExpression(SyntaxKind.NumericLiteralExpression, SF.Literal(number));
		}

		public static ArgumentSyntax ToArgument(this string number)
		{
			return SF.Argument(number.ToLiteral());
		}

		public static LiteralExpressionSyntax ToLiteral(this string value)
		{
			return SF.LiteralExpression(SyntaxKind.StringLiteralExpression, SF.Literal(value));
		}

		public static ArgumentSyntax ToArgument(this bool number)
		{
			return SF.Argument(number.ToLiteral());
		}

		public static LiteralExpressionSyntax ToLiteral(this bool value)
		{
			return (value) ? SF.LiteralExpression(SyntaxKind.TrueLiteralExpression) : SF.LiteralExpression(SyntaxKind.FalseLiteralExpression);
		}

		public static StringToken ToStringToken(this SyntaxToken token)
		{
			return ((StringToken)token.Text).AsInterpolation();
		}

		public static InterpolatedStringExpressionSyntax ToInterpolatedString(this StringToken value, params StringToken[] tokens)
		{
			var ltokens = new List<StringToken>
			{
				value
			};
			if (tokens != null)
			{
				ltokens.AddRange(tokens);
			}
			var ise = SF.InterpolatedStringExpression(SF.Token(SyntaxKind.InterpolatedStringStartToken));

			foreach (var token in ltokens)
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

		public static InterpolatedStringExpressionSyntax ToInterpolatedString(this string value, params StringToken[] tokens)
		{
			return ToInterpolatedString((StringToken)value, tokens);
		}
	}
}
