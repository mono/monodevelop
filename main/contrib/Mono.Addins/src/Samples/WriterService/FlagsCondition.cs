
using System;
using Mono.Addins;

namespace WriterService
{
	public class FlagsCondition: ConditionType
	{
		string[] flags;
		
		public FlagsCondition (string[] flags)
		{
			this.flags = flags;
		}
		
		public override bool Evaluate (NodeElement attributes)
		{
			string flag = attributes.GetAttribute ("value");
			return Array.IndexOf (flags, flag) != -1;
		}
	}
}
