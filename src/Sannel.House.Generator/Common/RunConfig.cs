using Sannel.House.Generator.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sannel.House.Generator.Common
{
    public class RunConfig
    {
		public String Name { get; set; }
		public String Directory { get; set; }
		public String FileName { get; set; }
		public ITestBuilder TestBuilder { get; set; }
		public IHttpClientBuilder HttpBuilder { get; set; }
		public ITaskBuilder TaskBuilder { get; set; }
		public Dictionary<String, String> Variables { get; set; } = new Dictionary<String, String>();
    }
}
