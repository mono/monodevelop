// 
// TextTransformation.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.VisualStudio.TextTemplating
{
	
	
	public abstract class TextTransformation : IDisposable
	{
		Stack<int> indents = new Stack<int> ();
		string currentIndent = "";
		CompilerErrorCollection errors = new CompilerErrorCollection ();
		StringBuilder builder = new StringBuilder ();
		
		public TextTransformation ()
		{
		}
		
		protected internal virtual void Initialize ()
		{
		}
		
		public abstract string TransformText ();
		
		#region Errors
		
		public void Error (string message)
		{
			AddError (message);
		}
		
		public void Warning (string message)
		{
			AddError (message).IsWarning = true;
		}
		
		CompilerError AddError (string message)
		{
			CompilerError err = new CompilerError ();
			err.Column = err.Line = -1;
			err.ErrorText = message;
			errors.Add (err);
			return err;
		}
		
		protected internal CompilerErrorCollection Errors {
			get { return errors; }
		}
		
		#endregion
		
		#region Indents
		
		public string PopIndent ()
		{
			if (indents.Count == 0)
				return "";
			int lastPos = currentIndent.Length - indents.Pop ();
			string last = currentIndent.Substring (lastPos);
			currentIndent = currentIndent.Substring (0, lastPos);
			return last; 
		}
		
		public void PushIndent (string indent)
		{
			indents.Push (indent.Length);
			currentIndent += indent;
		}
		
		public void ClearIndent ()
		{
			currentIndent = "";
			indents.Clear ();
		}
		
		public string CurrentIndent {
			get { return currentIndent; }
		}
		
		#endregion
		
		#region Writing
		
		protected StringBuilder GenerationEnvironment {
			get { return builder; }
			set {
				if (value == null)
					throw new ArgumentNullException ();
				builder = value;
			}
		}
		
		public void Write (string textToAppend)
		{
			GenerationEnvironment.Append (textToAppend);
		}
		
		public void Write (string format, params object[] args)
		{
			GenerationEnvironment.AppendFormat (format, args);
		}
		
		public void WriteLine (string textToAppend)
		{
			GenerationEnvironment.Append (CurrentIndent);
			GenerationEnvironment.AppendLine (textToAppend);
		}
		
		public void WriteLine (string format, params object[] args)
		{
			GenerationEnvironment.Append (CurrentIndent);
			GenerationEnvironment.AppendFormat (format, args);
			GenerationEnvironment.AppendLine ();
		}

		#endregion
		
		#region Dispose
		
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		
		protected virtual void Dispose (bool disposing)
		{
		}
		
		~TextTransformation ()
		{
			Dispose (false);
		}
		
		#endregion

	}
}
