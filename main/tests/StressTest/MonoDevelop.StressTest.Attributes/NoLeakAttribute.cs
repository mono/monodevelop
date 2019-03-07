using System;
namespace MonoDevelop.StressTest.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple=true, Inherited=true)]
	public class NoLeakAttribute : Attribute
	{
		public string TypeName { get; }

		public NoLeakAttribute(string typeName)
		{
			TypeName = typeName;
		}
	}
}
