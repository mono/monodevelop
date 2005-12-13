//
// BaseRefactorer.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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

using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.IO;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.CodeGeneration;

namespace MonoDevelop.Projects.CodeGeneration
{
	public abstract class BaseRefactorer: IRefactorer
	{
		public virtual RefactorOperations SupportedOperations {
			get { return RefactorOperations.All; }
		}
		
		protected abstract ICodeGenerator GetGenerator ();
	
		public IClass CreateClass (IRefactorerContext ctx, string directory, string namspace, CodeTypeDeclaration type)
		{
			CodeCompileUnit unit = new CodeCompileUnit ();
			CodeNamespace ns = new CodeNamespace (namspace);
			ns.Types.Add (type);
			unit.Namespaces.Add (ns);
			
			string file = Path.Combine (directory, type.Name + ".cs");
			StreamWriter sw = new StreamWriter (file);
			
			ICodeGenerator gen = GetGenerator ();
			gen.GenerateCodeFromCompileUnit (unit, sw, new CodeGeneratorOptions ());
			
			sw.Close ();
			
			IParseInformation pi = ctx.ParserContext.ParseFile (file);
			ClassCollection clss = ((ICompilationUnit)pi.BestCompilationUnit).Classes;
			if (clss.Count > 0)
				return clss [0];
			else
				throw new Exception ("Class creation failed. The parser did not find the created class.");
		}
		
		public virtual void RenameClass (IRefactorerContext ctx, IClass cls, string newName)
		{
		}
		
		public virtual void RenameClassReferences (IRefactorerContext ctx, string file, IClass cls, string newName)
		{
		}
		
		public IMethod AddMethod (IRefactorerContext ctx, IClass cls, CodeMemberMethod method)
		{
			IEditableTextFile buffer = ctx.GetFile (cls.Region.FileName);
			
			int line, col;
			int pos = GetNewMethodPosition (buffer, cls);
			
			buffer.GetLineColumnFromPosition (pos, out line, out col);
			string indent = GetLineIndent (buffer, line);
			
			string code = GenerateCodeFromMember (method);
			code = Indent (code, indent, false);
			
			buffer.InsertText (pos, code);
			
			IParseInformation pi = ctx.ParserContext.ParseFile (buffer.Name, buffer.Text);
			foreach (IClass rclass in ((ICompilationUnit)pi.BestCompilationUnit).Classes) {
				if (cls.Name == rclass.Name) {
					foreach (IMethod m in rclass.Methods)
						if (m.Region.BeginLine == line)
							return m;
				}
			}
			return null;
		}

		public virtual void RemoveMethod (IRefactorerContext ctx, IClass cls, IMethod method)
		{
		}
		
		public virtual void RenameMethod (IRefactorerContext ctx, IClass cls, IMethod method, string newName)
		{
		}
		
		public virtual void RenameMethodReferences (IRefactorerContext ctx, string fileName, IClass cls, IMethod method, string newName)
		{
		}
		
		public virtual IField AddField (IRefactorerContext ctx, IClass cls, CodeMemberField field)
		{
			IEditableTextFile buffer = ctx.GetFile (cls.Region.FileName);
			
			int pos = GetNewFieldPosition (buffer, cls);
			
			string code = GenerateCodeFromMember (field);
			
			int line, col;
			buffer.GetLineColumnFromPosition (pos, out line, out col);
			
			string indent = GetLineIndent (buffer, line);
			code = Indent (code, indent, false);
			
			buffer.InsertText (pos, code);
			
			IParseInformation pi = ctx.ParserContext.ParseFile (buffer.Name, buffer.Text);
			foreach (IClass rclass in ((ICompilationUnit)pi.BestCompilationUnit).Classes) {
				if (cls.Name == rclass.Name) {
					foreach (IField f in rclass.Fields)
						if (f.Region.BeginLine == line)
							return f;
				}
			}
			return null;
		}
		
		public virtual void RemoveField (IRefactorerContext ctx, IClass cls, IField field)
		{
		}
		
		public virtual void RenameField (IRefactorerContext ctx, IClass cls, IField field, string newName)
		{
		}
		
		public virtual void RenameFieldReferences (IRefactorerContext ctx, string fileName, IClass cls, IField field, string newName)
		{
		}

		
		protected virtual string GenerateCodeFromMember (CodeTypeMember member)
		{
			CodeTypeDeclaration type = new CodeTypeDeclaration ("temp");
			type.Members.Add (member);
			ICodeGenerator gen = GetGenerator ();
			StringWriter sw = new StringWriter ();
			gen.GenerateCodeFromType (type, sw, new CodeGeneratorOptions ());
			string code = sw.ToString ();
			int i = code.IndexOf ('{');
			int j = code.LastIndexOf ('}');
			code = code.Substring (i+1, j-i-1);
			return RemoveIndent (code);
		}
		
		protected string RemoveIndent (string code)
		{
			string[] lines = code.Split ('\n');
			int minInd = int.MaxValue;
			
			for (int n=0; n<lines.Length; n++) {
				string line = lines [n];
				for (int i=0; i<line.Length; i++) {
					char c = line [i];
					if (c != ' ' && c != '\t') {
						if (i < minInd)
							minInd = i;
						break;
					}
				}
			}
			
			if (minInd == int.MaxValue)
				minInd = 0;
			
			int firstLine = -1, lastLine = -1;
			
			for (int n=0; n<lines.Length; n++) {
				if (minInd >= lines[n].Length)
					continue;
					
				if (lines[n].Trim (' ','\t') != "") {
					if (firstLine == -1)
						firstLine = n;
					lastLine = n;
				}
				
				lines [n] = lines [n].Substring (minInd);
			}
			
			if (firstLine == -1)
				return "";
			
			return string.Join ("\n", lines, firstLine, lastLine - firstLine + 1);
		}
		
		protected string Indent (string code, string indent, bool indentFirstLine)
		{
			code = code.Replace ("\n", "\n" + indent);
			if (indentFirstLine)
				return indent + code;
			else
				return code;
		}
		
		protected virtual int GetNewFieldPosition (IEditableTextFile buffer, IClass cls)
		{
			if (cls.Fields.Count == 0) {
				int sp = buffer.GetPositionFromLineColumn (cls.BodyRegion.BeginLine, cls.BodyRegion.BeginColumn);
				int ep = buffer.GetPositionFromLineColumn (cls.BodyRegion.EndLine, cls.BodyRegion.EndColumn);
				string s = buffer.GetText (sp, ep);
				int i = s.IndexOf ('{');
				if (i == -1) return -1;
				i++;
				int pos = GetNextLine (buffer, sp + i);
				string ind = GetLineIndent (buffer, cls.BodyRegion.BeginLine);
				buffer.InsertText (pos, ind + "\t\n");
				return pos + ind.Length + 1;
			} else {
				IField f = cls.Fields [cls.Fields.Count - 1];
				int pos = buffer.GetPositionFromLineColumn (f.Region.EndLine, f.Region.EndColumn);
				pos = GetNextLine (buffer, pos);
				string ind = GetLineIndent (buffer, f.Region.EndLine);
				buffer.InsertText (pos, ind);
				return pos + ind.Length;
			}
		}
		
		protected virtual int GetNewMethodPosition (IEditableTextFile buffer, IClass cls)
		{
			if (cls.Methods.Count == 0) {
				int pos = GetNewFieldPosition (buffer, cls);
				int line, col;
				buffer.GetLineColumnFromPosition (pos, out line, out col);
				string ind = GetLineIndent (buffer, line);
				pos = GetNextLine (buffer, pos);
				buffer.InsertText (pos, ind);
				return pos + ind.Length;
			}
			else {
				IMethod m = cls.Methods [cls.Methods.Count - 1];
				int pos = buffer.GetPositionFromLineColumn (m.BodyRegion.EndLine, m.BodyRegion.EndColumn);
				pos = GetNextLine (buffer, pos);
				pos = GetNextLine (buffer, pos);
				string ind = GetLineIndent (buffer, m.Region.EndLine);
				buffer.InsertText (pos, ind);
				return pos + ind.Length;
			}
		}
		
		protected virtual int GetNextLine (IEditableTextFile buffer, int pos)
		{
			while (pos < buffer.Length) {
				string s = buffer.GetText (pos, pos + 1);
				if (s == "\n") {
					buffer.InsertText (pos + 1, "\n");
					return pos + 1;
				}
				if (s != " " && s == "\t") {
					buffer.InsertText (pos, "\n\n");
					return pos + 1;
				}
			}
			return pos;
		}
		
		protected string GetLineIndent (IEditableTextFile buffer, int line)
		{
			int pos = buffer.GetPositionFromLineColumn (line, 1);
			int ipos = pos;
			string s = buffer.GetText (pos, pos + 1);
			while ((s == " " || s == "\t") && pos < buffer.Length) {
				pos++;
				s = buffer.GetText (pos, pos + 1);
			}
			return buffer.GetText (ipos, pos);
		}
	}
}
