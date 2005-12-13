using System;
using System.Collections;
using System.Drawing;

namespace ICSharpCode.SharpRefactory.Parser
{
	public class ModifierCollection : CollectionBase
	{
		public int Add (Modifier item)
	    {
			if ((Code & item.Code) != 0)
				return -1;
			return List.Add(item);
		}
		
		public void Add (Modifier item, Parser parser)
		{
			if (Add (item) == -1)
				parser.Error("modifier " + item.Code + " already defined");
		}
		
		public void Add (ModifierCollection modifiers)
		{
			foreach (Modifier modifier in modifiers)
				Add (modifier);
		}
		
		public void Add (ModifierCollection modifiers, Parser parser)
		{
			foreach (Modifier modifier in modifiers)
				Add (modifier, parser);
		}
	      
	      public void Insert(int index, Modifier item)
	      {
	              List.Insert (index, item);
	      }
	      
	      public void Remove(Modifier item)
	      {
	              List.Remove(item);
	      }
	      
	      public bool Contains(Modifier item)
	      {
			return List.Contains(item);
	      }
	      
	      public bool Contains(ModifierFlags item)
	      {
	      		if ((Code & item) != 0)
					return true;
	            else
					return false;
	      }
	      
	      public int IndexOf(Modifier item)
	      {
	              return List.IndexOf(item);
	      }
	      
	      public void CopyTo(Modifier[] array, int index)
	      {
	              List.CopyTo(array, index);
	      }
	      
	      public Modifier this[int index]
	      {
	              get { return (Modifier)List[index]; }
	              set { List[index] = value; }
	      }

	      public ModifierFlags Code
	      {
	      	get
	      	{
	      		ModifierFlags code = ModifierFlags.None;
	      		foreach (Modifier modifier in this)
	      		{
	      			code |= modifier.Code;
	      		}
	      		
	      		return code;
	      	}
	      }
	      
	    public void Check (ModifierFlags allowed, Parser parser)
		{
			ModifierFlags wrong = Code & (allowed ^ ModifierFlags.All);
			if (wrong != ModifierFlags.None)
				parser.Error ("modifier(s) " + wrong + " not allowed here");

			if ((Code & (ModifierFlags.Sealed | ModifierFlags.Static)) == (ModifierFlags.Sealed | ModifierFlags.Static))
				parser.Error ("cannot be both static and sealed");
			if ((Code & (ModifierFlags.Abstract | ModifierFlags.Static)) == (ModifierFlags.Abstract | ModifierFlags.Static))
				parser.Error ("cannot be both static and abstract");
		}
		
		// maybe we should rename it to IsEmpty?
		public bool isNone
		{
			get
			{ return (Code == ModifierFlags.None); }
		}
	}
	
	//[Obsolete ("yep... update it to ModifierCollection :)")]
	public class Modifier
	{
		ModifierFlags code;
		Point start, end;
		
		public Modifier (ModifierFlags code, Point start, Point end)
		{
			this.code = code;
			this.start = start;
			this.end = end;
		}
		
		[Obsolete ("You sure? Just don't let me catch you!")]
		public Modifier (ModifierFlags code)
		{
			this.code = code;
		}
		
		public ModifierFlags Code
		{
			get
			{ return code; }
		}
		
		public Point Start
		{
			get
			{ return start; }
		}
		
		public Point End
		{
			get
			{ return end; }
		}
	}


	[Flags]
	public enum ModifierFlags
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
