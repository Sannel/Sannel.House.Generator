using Sannel.House.Web.Base.Interfaces;
using Sannel.House.Generator.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.IO;
using Sannel.House.Web.Base;
using Sannel.House.Generator.Interfaces;
using Newtonsoft.Json;

namespace Sannel.House.Generator
{
	public class Program
	{
		private static T createInstance<T>(String className)
		{
			try
			{
				var type = Type.GetType(className);
				if(type == null)
				{
					Console.Error.WriteLine($"Could not find the type {className}");
					return default(T);
				}
				return (T)Activator.CreateInstance(type);
			}
			catch(Exception ex)
			{
				Console.Error.WriteLine($"Error loading generator {className}: {ex}");
			}

			return default(T);
		}

		public static void Main(string[] args)
		{
			var path = "bin\\Generated";
			try
			{
				if (Directory.Exists(path))
				{
					Directory.Delete(path, true);
				}
				Directory.CreateDirectory(path);
			}
			catch (Exception) { }

			Console.WriteLine("Loading Configuration");
			var generatorConfiguration = File.ReadAllText("GeneratorConfiguration.json");
			var config = JsonConvert.DeserializeObject<Configuration>(generatorConfiguration);

			var perTypeGenerators = new Dictionary<String, IPerTypeGenerator>();
			var combinedGenerators = new Dictionary<String, ICombinedGenerator>();

			foreach(var gen in config.Generators)
			{
				Console.WriteLine($"Loading generator {gen.Key}");
				if(gen.Value.Type == GeneratorTypes.PerType)
				{
					var c = createInstance<IPerTypeGenerator>(gen.Value.Class);
					if(c != null)
					{
						perTypeGenerators.Add(gen.Key, c);
					}
				}
				else
				{
					var c = createInstance<ICombinedGenerator>(gen.Value.Class);
					if(c != null)
					{
						combinedGenerators.Add(gen.Key, c);
					}
				}
			}

			var testBuilders = new Dictionary<String, ITestBuilder>();

			foreach(var test in config.TestBuilders)
			{
				Console.WriteLine($"Loading TestBuilder {test.Key}");
				var c = createInstance<ITestBuilder>(test.Value.Class);
				if(c != null)
				{
					testBuilders.Add(test.Key, c);
				}
			}

			var httpBuilders = new Dictionary<String, IHttpClientBuilder>();
			foreach(var httpBuilder in config.HttpBuilders)
			{
				Console.WriteLine($"Loading HttpClientBuilder {httpBuilder.Key}");
				var c = createInstance<IHttpClientBuilder>(httpBuilder.Value.Class);
				if(c != null)
				{
					httpBuilders.Add(httpBuilder.Key, c);
				}
			}

			var taskBuilders = new Dictionary<String, ITaskBuilder>();
			foreach(var taskBuilder in config.TaskBuilders)
			{
				Console.WriteLine($"Loading TaskBuilder {taskBuilder.Key}");
				var c = createInstance<ITaskBuilder>(taskBuilder.Value.Class);
				if(c != null)
				{
					taskBuilders.Add(taskBuilder.Key, c);
				}
			}

			var propWithNames = new List<PropertyWithName>();
			var t = typeof(IDataContext);
			var ti = t.GetTypeInfo();
			var props = ti.GetProperties().Where(i => String.Compare(i.PropertyType.Name, "DbSet`1") == 0);
			foreach (var prop in props)
			{
				var first = prop.PropertyType.GenericTypeArguments.FirstOrDefault();
				if (first.Namespace.StartsWith("Sannel"))
				{
					var gen = prop.GetCustomAttribute<GenerationAttribute>();
					if (gen != null && gen.Ignore)
					{
						continue;
					}

					propWithNames.Add(new PropertyWithName()
					{
						PropertyName = prop.Name,
						Type = first
					});
				}
			}
			
			foreach(var run in config.Run)
			{
				var r = new RunConfig();
				r.Directory = run.Directory;
				r.FileName = run.FileName;
				r.Name = run.Name;
				r.Variables = run.Variables;
				if (perTypeGenerators.ContainsKey(run.Generator))
				{
					var generator = perTypeGenerators[run.Generator];
					if (run.TestBuilder != null && testBuilders.ContainsKey(run.TestBuilder))
					{
						r.TestBuilder = testBuilders[run.TestBuilder];
					}
					if (run.HttpBuilder != null && httpBuilders.ContainsKey(run.HttpBuilder))
					{
						r.HttpBuilder = httpBuilders[run.HttpBuilder];
					}
					if(run.TaskBuilder != null && taskBuilders.ContainsKey(run.TaskBuilder))
					{
						r.TaskBuilder = taskBuilders[run.TaskBuilder];
					}
					foreach(var pwn in propWithNames)
					{
						Console.WriteLine($"\tGenerating {r.Name} {pwn.PropertyName}");
						generator.Generate(pwn, path, r);
					}
				}
				else if (combinedGenerators.ContainsKey(run.Generator))
				{
					var generator = combinedGenerators[run.Generator];
					if (run.TestBuilder != null && testBuilders.ContainsKey(run.TestBuilder))
					{
						r.TestBuilder = testBuilders[run.TestBuilder];
					}
					if (run.HttpBuilder != null && httpBuilders.ContainsKey(run.HttpBuilder))
					{
						r.HttpBuilder = httpBuilders[run.HttpBuilder];
					}
					if(run.TaskBuilder != null && taskBuilders.ContainsKey(run.TaskBuilder))
					{
						r.TaskBuilder = taskBuilders[run.TaskBuilder];
					}
					generator.Generate(propWithNames, path, r);
				}
			}
		}
	}
}
