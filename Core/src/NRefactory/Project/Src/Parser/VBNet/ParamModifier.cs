// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision: 915 $</version>
// </file>

using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Parser.VB
{
	internal class ParamModifiers
	{
		ICSharpCode.NRefactory.Parser.AST.ParamModifier cur;
		Parser   parser;
		
		public ICSharpCode.NRefactory.Parser.AST.ParamModifier Modifier {
			get {
				return cur;
			}
		}
		
		public ParamModifiers(Parser parser)
		{
			this.parser = parser;
			cur         = ICSharpCode.NRefactory.Parser.AST.ParamModifier.None;
		}
		
		public bool isNone { get { return cur == ICSharpCode.NRefactory.Parser.AST.ParamModifier.None; } }
		
		public void Add(ICSharpCode.NRefactory.Parser.AST.ParamModifier m) 
		{
			if ((cur & m) == 0) {
				cur |= m;
			} else {
				parser.Error("param modifier " + m + " already defined");
			}
		}
		
		public void Add(ParamModifiers m)
		{
			Add(m.cur);
		}
		
		public void Check()
		{
			if((cur & ICSharpCode.NRefactory.Parser.AST.ParamModifier.In) != 0 && 
			   (cur & ICSharpCode.NRefactory.Parser.AST.ParamModifier.Ref) != 0) {
				parser.Error("ByRef and ByVal are not allowed at the same time.");
			}
		}
	}
}
