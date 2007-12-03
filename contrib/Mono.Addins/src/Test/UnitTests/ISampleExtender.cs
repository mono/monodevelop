
using System;
using Mono.Addins;

namespace SimpleApp
{
	[TypeExtensionPoint]
	public interface ISampleExtender
	{
		string Text { get; }
	}
}
