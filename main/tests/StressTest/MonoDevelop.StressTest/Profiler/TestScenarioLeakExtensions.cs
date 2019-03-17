using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoDevelop.StressTest.Attributes;

namespace MonoDevelop.StressTest
{
	public static class TestScenarioLeakExtensions
	{
		const string autoTest = "MonoDevelop.Components.AutoTest.Results.GtkWidgetResult";

		public static HashSet<string> GetTrackedTypes (this ITestScenario scenario)
		{
			var result = new HashSet<string> ();
			var scenarioType = scenario.GetType ();

			foreach (var attr in scenarioType.GetCustomAttributes<NoLeakAttribute> (true)) {
				result.Add (attr.TypeName);
			}

			foreach (var attr in scenarioType.GetMethod("Run").GetCustomAttributes<NoLeakAttribute> (true)) {
				result.Add (attr.TypeName);
			}

			result.Add (autoTest);

			return result;
		}

		public static Dictionary<string, NoLeakAttribute> GetLeakAttributes (this ITestScenario scenario, bool isCleanup)
		{
			var scenarioType = scenario.GetType ();

			// If it's targeting the class, check on cleanup iteration, otherwise, check the run method.
			var member = isCleanup ? scenarioType : (MemberInfo)scenarioType.GetMethod ("Run");

			var attributes = member.GetCustomAttributes<NoLeakAttribute> (true).ToDictionary (x => x.TypeName, x => x);

			// TODO: Ensure that we don't leak, so add GtkWidgetResult results, as they can cause retention of UI widgets.
			attributes.Add (autoTest, new NoLeakAttribute (autoTest));

			return attributes;
		}
	}
}
