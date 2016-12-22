using System;
using System.Collections.Generic;
using System.Text;

namespace Sannel.House.Generator.Common
{
    public class RunConfiguration
    {
		public String Name { get; set; }
		public String Directory { get; set; }
		public String FileName { get; set; }
		public String Generator { get; set; }
		public String TestBuilder { get; set; }
		public String HttpBuilder { get; set; }

		public String TaskBuilder { get; set; }
		public Dictionary<String, String> Variables { get; set; } = new Dictionary<String, String>();
										   /*"Name":  "Controller",
											   "Directory":  "Web\\Controllers",
											   "FileName":  "{TypeName}Controller.cs",
											   "Generator":  "ControllerGenerator",
											   "TestBuilder": null,
											   "HttpBuilder": null*/
	}
}
