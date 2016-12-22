using Sannel.House.Generator.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sannel.House.Generator.Common
{
	public class HttpClientBuilder : IHttpClientBuilder
	{
		public string[] Namespace
		{
			get
			{
				return new String[]
				{
					"System.Net",
					"System.Net.Http"
				};
			}
		}

		public string GetStatusCode(string code)
		{
			if(String.Compare(code, "ok", true) == 0)
			{
				return "OK";
			}

			return code;
		}
	}
}
