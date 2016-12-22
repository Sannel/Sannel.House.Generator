using System;
using System.Collections.Generic;
using System.Text;

namespace Sannel.House.Generator.Common
{
    public class Configuration
    {
		public Dictionary<String, GeneratorConfiguration> Generators
		{
			get;
			set;
		} = new Dictionary<String, GeneratorConfiguration>();

		public Dictionary<String, GeneratorConfiguration> TestBuilders
		{
			get;
			set;
		} = new Dictionary<string, GeneratorConfiguration>();

		public Dictionary<String, GeneratorConfiguration> HttpBuilders
		{
			get;
			set;
		} = new Dictionary<string, GeneratorConfiguration>();

		public Dictionary<String, GeneratorConfiguration> TaskBuilders
		{
			get;
			set;
		} = new Dictionary<string, GeneratorConfiguration>();

		public List<RunConfiguration> Run
		{
			get;
			set;
		} = new List<RunConfiguration>();
    }
}
