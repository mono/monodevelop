using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LeakTest
{
	public static class TestScenarioLeakExtensions
	{
		public static HashSet<string> GetTrackedTypes (this ITestScenario scenario, Type[] extraTypes = null)
		{
			var result = new HashSet<string> ();
			var scenarioType = scenario.GetType ();

			foreach (var attr in scenarioType.GetCustomAttributes<NoLeakAttribute> (true)) {
				result.Add (attr.TypeName);
			}

			foreach (var attr in scenarioType.GetMethod (nameof (ITestScenario.Run)).GetCustomAttributes<NoLeakAttribute> (true)) {
				result.Add (attr.TypeName);
			}

			if (extraTypes != null) {
				foreach (var type in extraTypes) {
					result.Add (type.FullName);
				}
			}

			return result;
		}

		public static Dictionary<string, NoLeakAttribute> GetLeakAttributes (this ITestScenario scenario, bool isCleanup, Type[] extraTypes = null)
		{
			var scenarioType = scenario.GetType ();

			// If it's targeting the class, check on cleanup iteration, otherwise, check the run method.
			var member = isCleanup ? scenarioType : (MemberInfo)scenarioType.GetMethod (nameof (ITestScenario.Run));

			var attributes = member.GetCustomAttributes<NoLeakAttribute> (true).ToDictionary (x => x.TypeName, x => x);

			// Ensure that we don't leak, so add AutoTest results, as they can cause retention of UI widgets.
			if (extraTypes != null) {
				foreach (var type in extraTypes) {
					attributes.Add (type.FullName, new NoLeakAttribute (type));
				}
			}

			return attributes;
		}
	}
}
