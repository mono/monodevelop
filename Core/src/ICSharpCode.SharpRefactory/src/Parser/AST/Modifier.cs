using System;

namespace ICSharpCode.SharpRefactory.Parser
{
	[Flags]
	public enum Modifier
	{
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
		
		// Modifier scopes
		None      = 0x0000,
		
		Classes                         = New | Public | Protected | Internal | Private | Abstract | Sealed | Static | Unsafe,
		Fields                          = New | Public | Protected | Internal | Private | Static   | Readonly | Volatile | Unsafe,
		PropertysEventsMethods          = New | Public | Protected | Internal | Private | Static   | Virtual  | Sealed   | Override | Abstract | Extern | Unsafe,
		Indexers                        = New | Public | Protected | Internal | Private | Virtual  | Sealed   | Override | Abstract | Extern,
		Operators                       = Public | Static | Extern,
		Constants                       = New | Public | Protected | Internal | Private,
		// FIXME: unsafe is not valid for enums
		StructsInterfacesEnumsDelegates = New | Public | Protected | Internal | Private | Unsafe,
		StaticConstructors              = Extern | Static | Unsafe,
		Destructors                     = Extern | Unsafe,
		Constructors                    = Public | Protected | Internal | Private | Extern,
		
		All       = Private  | Internal | Protected | Public |
		            Abstract | Virtual  | Sealed    | Static | 
		            Override | Readonly | Const     | New    |
		            Extern   | Volatile | Unsafe
	}
}
