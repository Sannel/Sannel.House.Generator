/* Copyright 2016 Sannel Software, L.L.C.

   Licensed under the Apache License, Version 2.0 (the ""License"");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

	   http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an ""AS IS"" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sannel.House.Generator.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SF = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Sannel.House.Generator.Common
{
	public abstract class GeneratorBase : IPerTypeGenerator
	{
		protected ITestBuilder TestBuilder
		{
			get;
			private set;
		}

		protected IHttpClientBuilder HttpBuilder
		{
			get;
			private set;
		} 

		public void Generate(PropertyWithName pwn, String baseSaveFolder, RunConfig config)
		{
			TestBuilder = config.TestBuilder;
			HttpBuilder = config.HttpBuilder;

			var sf = Path.Combine(baseSaveFolder, config.Directory.ReplaceKeys(pwn, config));
			if (!Directory.Exists(sf))
			{
				Directory.CreateDirectory(sf);
			}

			var syntax = internalGenerate(pwn.PropertyName, pwn.Type).NormalizeWhitespace("\t", true);

			var file = Path.Combine(sf, config.FileName.ReplaceKeys(pwn, config));
			if (File.Exists(file))
			{
				File.Delete(file);
			}
			using (StreamWriter writer = new StreamWriter(File.OpenWrite(file)))
			{
				syntax.WriteTo(writer);
				//formattedNode.WriteTo(writer);
			}
		}


		public static SyntaxTrivia GetLicenseComment()
		{
			var comment = SF.Comment($@"/* Copyright {DateTime.Now.Year} Sannel Software, L.L.C.

   Licensed under the Apache License, Version 2.0 (the ""License"");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

	   http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an ""AS IS"" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.*/
");
			return comment;
		}

		protected abstract CompilationUnitSyntax internalGenerate(String propertyName, Type t);
	}
}
