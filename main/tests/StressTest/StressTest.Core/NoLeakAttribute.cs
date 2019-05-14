using System;
namespace LeakTest
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
