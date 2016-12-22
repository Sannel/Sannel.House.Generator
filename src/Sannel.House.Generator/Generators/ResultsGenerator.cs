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

namespace Sannel.House.Generator.Generators
{
	public class ResultsGenerator : ResultGenerator
	{
		protected override string GetClassName(Type t)
		{
			return $"{t.Name}Results";
		}

		protected override TypeSyntax getDataType(Type t)
		{
			return SF.ParseTypeName($"IList<I{t.Name}>");
		}
	}
}
