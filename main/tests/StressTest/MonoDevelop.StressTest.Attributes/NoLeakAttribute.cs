using System;
namespace MonoDevelop.StressTest.Attributes
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
	public class NoLeakAttribute : Attribute
	{
		public string TypeName { get; }

		public NoLeakAttribute (Type type) : this(type.FullName)
		{
		}

		public NoLeakAttribute(string typeName)
		{
			TypeName = typeName;
		}

		// Add configurable parameters:
		// Ignore objects found in the initial heapshot
	}
}
