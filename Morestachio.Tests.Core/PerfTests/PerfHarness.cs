using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Morestachio.Tests.PerfTests
{
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

			MorestachioDocumentInfo template = null;

			//make sure this class is JIT'd before we start timing.
			Parser.ParseWithOptions(new ParserOptions("asdf"));

			var totalTime = Stopwatch.StartNew();
			var parseTime = Stopwatch.StartNew();
			Stopwatch renderTime;
			for (var i = 0; i < runs; i++)
			{
				template = Parser.ParseWithOptions(new ParserOptions(baseTemplate, () => Stream.Null));
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