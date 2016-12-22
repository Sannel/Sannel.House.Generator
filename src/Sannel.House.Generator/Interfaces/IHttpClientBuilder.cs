using System;
using System.Collections.Generic;
using System.Text;

namespace Sannel.House.Generator.Interfaces
{
    public interface IHttpClientBuilder
    {
		String[] Namespace
		{
			get;
		}

		String GetStatusCode(String code);
    }
}
