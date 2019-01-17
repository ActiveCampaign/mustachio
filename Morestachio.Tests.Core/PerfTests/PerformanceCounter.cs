using System;
using System.Collections.Generic;
using System.IO;
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
			File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + @"\MorestachioPerf.csv", output.ToString());
		}
	}
}