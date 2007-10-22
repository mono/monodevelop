
using System;
using Mono.Addins;

namespace SimpleApp
{
	public class GlobalInfoCondition: ConditionType
	{
		static string val;
		public static GlobalInfoCondition Instance;
		
		public static string Value {
			get { return val; }
			set { val = value; if (Instance != null) Instance.NotifyChanged (); }
		}
		
		public GlobalInfoCondition ()
		{
			Instance = this;
		}
		
		public override bool Evaluate (NodeElement condition)
		{
			return condition.GetAttribute ("value") == Value;
		}
	}
}
