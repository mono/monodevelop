
using System;
using Mono.Addins;

namespace SimpleApp
{
	public class ParameterInfoCondition: ConditionType
	{
		string val;
		
		public string Value {
			get { return val; }
			set { val = value; NotifyChanged (); }
		}
		
		public override bool Evaluate (NodeElement condition)
		{
			return val == condition.GetAttribute ("value");
		}
	}
}
