using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MonoDevelop.Components.AutoTest.Results;
using MonoDevelop.StressTest.Attributes;

namespace MonoDevelop.StressTest
{
	public static class TestScenarioLeakExtensions
	{
		static readonly Type[] resultTypes = {
			typeof(GtkWidgetResult),
			typeof(NSObjectResult),
		};

		public static HashSet<string> GetTrackedTypes (this ITestScenario scenario)
		{
			var result = new HashSet<string> ();
			var scenarioType = scenario.GetType ();

			foreach (var attr in scenarioType.GetCustomAttributes<NoLeakAttribute> (true)) {
				result.Add (attr.TypeName);
			}

			foreach (var attr in scenarioType.GetMethod (nameof (ITestScenario.Run)).GetCustomAttributes<NoLeakAttribute> (true)) {
				result.Add (attr.TypeName);
			}

			foreach (var type in resultTypes) {
				result.Add (type.FullName);
			}

			return result;
		}

		public static Dictionary<string, NoLeakAttribute> GetLeakAttributes (this ITestScenario scenario, bool isCleanup)
		{
			var scenarioType = scenario.GetType ();

			// If it's targeting the class, check on cleanup iteration, otherwise, check the run method.
			var member = isCleanup ? scenarioType : (MemberInfo)scenarioType.GetMethod (nameof (ITestScenario.Run));

			var attributes = member.GetCustomAttributes<NoLeakAttribute> (true).ToDictionary (x => x.TypeName, x => x);

			// Ensure that we don't leak, so add AutoTest results, as they can cause retention of UI widgets.
			foreach (var type in resultTypes) {
				attributes.Add (type.FullName, new NoLeakAttribute (type));
			}

			return attributes;
		}
	}
}
