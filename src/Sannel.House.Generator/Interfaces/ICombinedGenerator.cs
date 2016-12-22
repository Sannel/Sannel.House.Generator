using Sannel.House.Generator.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Sannel.House.Generator.Interfaces
{
    public interface ICombinedGenerator
    {
		void Generate(IList<PropertyWithName> props, String baseSaveDirectory, RunConfig config);
    }
}
