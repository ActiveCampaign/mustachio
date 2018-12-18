using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Morestachio.Tests.PerfTests
{
	[SetUpFixture]
	public class PerformanceCounter
	{
		public class PerformanceCounterEntity
		{
			public PerformanceCounterEntity(string name)
			{
				Name = name;
			}

			public string Name { get; private set; }
			public TimeSpan TimePerRun { get; set; }
			public int RunOver { get; set; }
			public int ModelDepth { get; set; }
			public int SubstitutionCount { get; set; }
			public int TemplateSize { get; set; }
			public TimeSpan ParseTime { get; set; }
			public TimeSpan RenderTime { get; set; }
			public TimeSpan TotalTime { get; set; }

			public string PrintAsCsv()
			{
				return
					$"{Name}, {TimePerRun:c}, {RunOver}, {ModelDepth}, {SubstitutionCount}, {TemplateSize}, {ParseTime:c}, {RenderTime:c}, {TotalTime:c}";
			}
		}

		public static ICollection<PerformanceCounterEntity> PerformanceCounters { get; private set; }

		[OneTimeSetUp]
		public void PerfStart()
		{
			PerformanceCounters = new List<PerformanceCounterEntity>();
		}

		[OneTimeTearDown]
		public void PrintPerfCounter()
		{
			var output = new StringBuilder();
			//Console.WriteLine(
			//	"Variation: '{8}', Time/Run: {7}ms, Runs: {0}x, Model Depth: {1}, SubstitutionCount: {2}," +
			//	" Template Size: {3}, ParseTime: {4}, RenderTime: {5}, Total Time: {6}",
			//	runs, modelDepth, inserts, sizeOfTemplate, parseTime.Elapsed, renderTime.Elapsed, totalTime.Elapsed,
			//	totalTime.ElapsedMilliseconds / (double) runs, variation);

			output.AppendLine("Variation, Time/Run, Runs, Model Depth, SubstitutionCount, Template Size(byte), ParseTime, RenderTime, Total Time");
			foreach (var performanceCounter in PerformanceCounters)
			{
				output.AppendLine(performanceCounter.PrintAsCsv());
			}

			Console.WriteLine(output.ToString());
			TestContext.Progress.WriteLine(output.ToString());
		}
	}


	[TestFixture]
	public class PerfHarness
	{
		

		[Test()]
		[Explicit]
		//[Theory(Skip = "Explicit Performance testing only")]
		[Category("Explicit")]
		[TestCase("Model Depth", 5, 30000, 10, 5000)]
		[TestCase("Model Depth", 10, 30000, 10, 5000)]
		[TestCase("Model Depth", 100, 30000, 10, 5000)]
		[TestCase("Substitutions", 5, 30000, 10, 5000)]
		[TestCase("Substitutions", 5, 30000, 50, 5000)]
		[TestCase("Substitutions", 5, 30000, 100, 5000)]
		[TestCase("Template Size", 5, 15000, 10, 5000)]
		[TestCase("Template Size", 5, 25000, 10, 5000)]
		[TestCase("Template Size", 5, 30000, 10, 5000)]
		[TestCase("Template Size", 5, 50000, 10, 5000)]
		[TestCase("Template Size", 5, 100000, 10, 5000)]
		public void TestRuns(string variation, int modelDepth, int sizeOfTemplate, int inserts, int runs)
		{
			var model = ConstructModelAndPath(modelDepth);
			var baseTemplate = Enumerable.Range(1, 5)
				.Aggregate("", (seed, current) => seed += " {{" + model.Item2 + "}}");
			while (baseTemplate.Length <= sizeOfTemplate)
			{
				baseTemplate += model.Item2 + "\r\n";
			}

			ExtendedParseInformation template = null;

			//make sure this class is JIT'd before we start timing.
			Parser.ParseWithOptions(new ParserOptions("asdf"));

			var totalTime = Stopwatch.StartNew();
			var parseTime = Stopwatch.StartNew();
			Stopwatch renderTime;
			for (var i = 0; i < runs; i++)
			{
				template = Parser.ParseWithOptions(new ParserOptions(baseTemplate, () => new MemoryStream()));
			}

			parseTime.Stop();

			renderTime = Stopwatch.StartNew();
			for (var i = 0; i < runs; i++)
			{
				using (var f = template.CreateAsync(model.Item1))
				{
				}
			}

			renderTime.Stop();
			totalTime.Stop();

			PerformanceCounter.PerformanceCounters.Add(new PerformanceCounter.PerformanceCounterEntity(variation)
			{
				TimePerRun = new TimeSpan(totalTime.ElapsedTicks / runs),
				RunOver = runs,
				ModelDepth = modelDepth,
				SubstitutionCount = inserts,
				TemplateSize = sizeOfTemplate,
				ParseTime = parseTime.Elapsed,
				RenderTime = renderTime.Elapsed,
				TotalTime = totalTime.Elapsed
			});
		}

		private Tuple<Dictionary<string, object>, string> ConstructModelAndPath(int modelDepth, string path = null)
		{
			path = Guid.NewGuid().ToString("n");
			var model = new Dictionary<string, object>();

			if (modelDepth > 1)
			{
				var child = ConstructModelAndPath(modelDepth - 1, path);
				model[path] = child.Item1;
				path = path + "." + child.Item2;
			}

			return Tuple.Create(model, path);
		}
	}
}