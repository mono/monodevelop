using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoDevelop.StressTest.Attributes;

namespace MonoDevelop.StressTest.MonoDevelop.StressTest.Profiler
{
	public static class TestScenarioLeakExtensions
	{
		public static Dictionary<string, NoLeakAttribute> GetLeakAttributes (this ITestScenario scenario, bool isCleanup)
		{
			var scenarioType = scenario.GetType ();

			// If it's targeting the class, check on cleanup iteration, otherwise, check the run method.
			var member = isCleanup ? scenarioType : (MemberInfo)scenarioType.GetMethod ("Run");

			var attributes = member.GetCustomAttributes<NoLeakAttribute> (true).ToDictionary (x => x.TypeName, x => x);

			// TODO: Ensure that we don't leak, so add GtkWidgetResult results, as they can cause retention of UI widgets.
			// attributes.Add("MonoDevelop.Components.AutoTest.Results.GtkWidgetResult

			return attributes;
		}
	}
}
