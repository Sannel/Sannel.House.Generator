using Sannel.House.Generator.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sannel.House.Generator.Common
{
	public class UWPHttpClientBuilder : IHttpClientBuilder
	{
		public String[] Namespace
		{
			get
			{
				return new String[]
				{
					"Windows.Web.Http",
					"Windows.Web.Http.Filters"
				};
			}
		}

		public string GetStatusCode(string code)
		{
			if(String.Compare(code, "ok", true) == 0)
			{
				return "Ok";
			}
			return code;
		}
	}
}
