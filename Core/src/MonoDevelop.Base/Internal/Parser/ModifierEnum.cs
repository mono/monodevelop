// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>
using System;

namespace MonoDevelop.Internal.Parser
{
	[Flags]
	public enum ModifierEnum : uint {
		None       = 0,
		
		// Access 
		Private   = 0x0001,
		Internal  = 0x0002,
		Protected = 0x0004,
		Public    = 0x0008,
		
		// Scope
		Abstract  = 0x0010, 
		Virtual   = 0x0020,
		Sealed    = 0x0040,
		Static    = 0x0080,
		Override  = 0x0100,
		Readonly  = 0x0200,
		Const	  = 0X0400,
		New       = 0x0800,
		
		// Special 
		Extern    = 0x1000,
		Volatile  = 0x2000,
		Unsafe    = 0x4000,
		
		ProtectedAndInternal = Internal | Protected,
		ProtectedOrInternal = 0x8000,
//		Literal             = 0x10000, <-- == Const now!!!
		SpecialName         = 0x20000,
		
		Final               = 0x40000,
	}
}

