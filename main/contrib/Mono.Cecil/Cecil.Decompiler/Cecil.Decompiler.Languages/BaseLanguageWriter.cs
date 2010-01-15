#region license
//
//	(C) 2007 - 2008 Novell, Inc. http://www.novell.com
//	(C) 2007 - 2008 Jb Evain http://evain.net
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using Mono.Cecil;

using Cecil.Decompiler.Ast;

namespace Cecil.Decompiler.Languages {

	public abstract class BaseLanguageWriter : BaseCodeVisitor, ILanguageWriter {

		protected ILanguage language;
		protected IFormatter formatter;

		public BaseLanguageWriter (ILanguage language, IFormatter formatter)
		{
			this.language = language;
			this.formatter = formatter;
		}

		protected void WriteToken (string token)
		{
			formatter.WriteToken (token);
		}

		protected void WriteSpace ()
		{
			formatter.WriteSpace ();
		}

		protected void WriteLine ()
		{
			formatter.WriteLine ();
		}

		protected void WriteKeyword (string keyword)
		{
			formatter.WriteKeyword (keyword);
		}

		protected void Write (string str)
		{
			formatter.Write (str);
		}

		protected void WriteLiteral (string literal)
		{
			formatter.WriteLiteral (literal);
		}

		protected void WriteIdentifier (string name, object identifier)
		{
			formatter.WriteIdentifier (name, identifier);
		}

		protected void WriteDefinition (string name, object definition)
		{
			formatter.WriteDefinition (name, definition);
		}

		protected void WriteReference (string name, object reference)
		{
			formatter.WriteReference (name, reference);
		}

		protected void Indent ()
		{
			formatter.Indent ();
		}

		protected void Outdent ()
		{
			formatter.Outdent ();
		}

		public abstract void Write (MethodDefinition method);
		public abstract void Write (Statement statement);
		public abstract void Write (Expression expression);
	}
}
