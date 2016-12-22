using Sannel.House.Generator.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sannel.House.Generator.Interfaces
{
    public interface IPerTypeGenerator
    {
		/// <summary>
		/// Generates the specified property name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="type">The type.</param>
		/// <param name="saveFolder">The save folder.</param>
		void Generate(PropertyWithName pwn, String baseSaveDirectory, RunConfig config);
    }
}
