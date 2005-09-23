// ExternAliasDeclaration.cs 
//
// Author: John Luke  <john.luke@gmail.com>
//

using System;

namespace ICSharpCode.SharpRefactory.Parser.AST
{
	public class ExternAliasDeclaration : AbstractNode
	{
		string alias;

		public ExternAliasDeclaration (string alias)
		{
			this.alias = alias;
		}

		public string Alias {
			get { return alias; }
			set { alias = value; }
		}

		public override object AcceptVisitor (IASTVisitor visitor, object data)
		{
			return visitor.Visit (this, data);
		}

		public override string ToString ()
		{
			return String.Format ("[ExternAliasDeclaration: Alias = {0}]", alias);
		}
	}
}

