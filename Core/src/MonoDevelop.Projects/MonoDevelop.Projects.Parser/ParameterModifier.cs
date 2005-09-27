// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

namespace MonoDevelop.Projects.Parser
{
	[Serializable]
	[Flags]
	public enum ParameterModifier : byte {
		None   = 0,
		Out    = (1 << 0),
		Ref    = (1 << 1),
		Params = (1 << 2)
	}
}
