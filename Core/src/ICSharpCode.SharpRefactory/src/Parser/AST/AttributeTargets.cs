using System;
using System.Collections;
using System.Text;

namespace ICSharpCode.SharpRefactory.Parser
{
	[Flags]
	public enum AttributeTarget
	{
		Assembly = 0x0001,
		Field    = 0x0002,
		Event    = 0x0004,
		Method   = 0x0008,
		Module   = 0x0010,
		Param    = 0x0020,
		Property = 0x0040,
		Return   = 0x0080,
		Type     = 0x0100
	}
	
}
