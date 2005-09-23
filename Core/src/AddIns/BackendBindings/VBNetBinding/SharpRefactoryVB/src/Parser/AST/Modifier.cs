using System;

namespace ICSharpCode.SharpRefactory.Parser.VB
{

	[Flags()]
	public enum ParamModifier
	{
		None						= 0x0000,	// 0
		ByVal						= 0x0001,	// 1
		ByRef						= 0x0002,	// 2
		ParamArray					= 0x0004,	// 4
		Optional					= 0x0008,	// 8
		All							= ByVal | ByRef | ParamArray | Optional
	}

	[Flags()]
	public enum Modifier
	{
		None						= 0x0000,	// 0
		
		// Access
		Private						= 0x0001,	// 1
		Friend						= 0x0002,	// 2
		Protected					= 0x0004,	// 4
		Public						= 0x0008,	// 8
		Dim							= 0x0010,	// 16
		
		// Scope
		Shadows						= 0x000020,	// 32
		Overloads					= 0x000040,	// 64
		Overrides					= 0x000080,	// 128
		NotOverridable				= 0x000100,	// 256
		MustOverride				= 0x000200,	// 512
		MustInherit					= 0x000400,	// 1024
		NotInheritable				= 0x000800,	// 2048
		Shared						= 0x001000,	// 4096
		Overridable					= 0x002000,	// 8192
		Constant					= 0x004000,
		// Methods and properties
		WithEvents					= 0x008000,
		ReadOnly					= 0x010000,
		WriteOnly					= 0x020000,
		Default						= 0x040000,
		
		// local variables
		Static						= 0x100000,
		
		All							= Private | Public | Protected  | Friend | Shadows | Constant |
									  Overloads | Overrides | NotOverridable | MustOverride |
									  MustInherit | NotInheritable | Shared | Overridable |
									  WithEvents | ReadOnly | WriteOnly | Default | Dim,
		
		Classes						= Private | Public | Protected | Friend | Shadows | MustInherit | NotInheritable,
		Structures					= Private | Public | Protected | Friend | Shadows,
		Enums						= Private | Public | Protected | Friend | Shadows,
		Modules						= Private | Public | Protected | Friend,
		Interfaces					= Private | Public | Protected | Friend | Shadows,
		Delegates					= Private | Public | Protected | Friend | Shadows,
		Methods						= Private | Public | Protected | Friend | Shadows | Shared | Overridable | NotOverridable | MustOverride | Overrides | Overloads,
		ExternalMethods				= Private | Public | Protected | Friend | Shadows | Overloads,
		Constructors				= Private | Public | Protected | Friend | Shared,
		Events						= Private | Public | Protected | Friend | Shadows | Shared,
		Constants					= Private | Public | Protected | Friend | Shadows,
		Fields						= Private | Public | Protected | Friend | Shadows | Shared | ReadOnly | WithEvents | Dim ,
		Properties					= Private | Public | Protected | Friend | Shadows | Shared | Overridable | NotOverridable | MustOverride | Overrides | Overloads | Default | ReadOnly | WriteOnly,
		
		// this is not documented in the spec
		InterfaceEvents				= Shadows,
		InterfaceMethods			= Shadows | Overloads,
		InterfaceProperties			= Shadows | Overloads | ReadOnly | WriteOnly | Default,
		InterfaceEnums				= Shadows,
	}
}
