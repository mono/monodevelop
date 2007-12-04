
using System;
using Mono.Addins;

namespace FileContentExtension
{
	[TypeExtensionPoint]
	public interface IExtraFileContent
	{
		string Content { get; }
	}
}
