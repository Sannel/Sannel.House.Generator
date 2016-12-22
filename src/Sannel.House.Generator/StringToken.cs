using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sannel.House.Generator
{
	public struct StringToken
	{
		public bool IsInterpolation { get; set; }

		public String Value { get; set; }

		public StringToken AsInterpolation()
		{
			IsInterpolation = true;
			return this;
		}


		public static implicit operator StringToken(String value)
		{
			return new StringToken()
			{
				Value = value
			};
		}
	}
}
