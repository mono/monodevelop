// 
// CodebehindTemplateBase.cs
//  
// Author:
//	   Michael Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2011 Michael Hutchinson
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
using System.Text;
using System.Collections.Generic;

namespace MonoDevelop.MacDev.ObjCIntegration
{
	public abstract class CodebehindTemplateBase
	{
		StringBuilder builder;
		ToStringHelperClass toStringHelper = new ToStringHelperClass ();
		
		public StringBuilder GenerationEnvironment {
			get { return builder ?? (builder = new StringBuilder ()); }
			set { builder = value; }
		}
		
		public void Write (string textToAppend)
		{
			this.GenerationEnvironment.Append (textToAppend);
		}
		
		public void Write (string format, params object[] args)
		{
			this.GenerationEnvironment.AppendFormat (format, args);
		}
		
		public void WriteLine (string textToAppend)
		{
			this.GenerationEnvironment.AppendLine (textToAppend);
		}
		
		public void WriteLine (string format, params object[] args)
		{
			this.GenerationEnvironment.AppendFormat (format, args);
			this.GenerationEnvironment.AppendLine ();
		}
		
		protected ToStringHelperClass ToStringHelper {
			get {
				return toStringHelper;
			}
		}
		
		protected class ToStringHelperClass
		{
			public string ToStringWithCulture (object objectToConvert)
			{
				return (string) objectToConvert;
			}
		}
		
		protected virtual void Initialize ()
		{
		}
		
		public abstract string TransformText ();
		
		public string WrapperNamespace { get; set; }
		public System.CodeDom.Compiler.CodeDomProvider Provider { get; set; }
		public List<NSObjectTypeInfo> Types { get; set; }
		
		protected string GetNs (string fullname)
		{
			var dotIdx = fullname.LastIndexOf ('.');
			if (dotIdx > 0) {
				return fullname.Substring (0, dotIdx);
			}
			return null;
		}
		
		protected string GetName (string fullname)
		{
			var dotIdx = fullname.LastIndexOf ('.');
			if (dotIdx > 0) {
				return fullname.Substring (dotIdx + 1);
			} else {
				return fullname;
			}
		}
		
		protected string EscapeIdentifier (string unescaped)
		{
			return Provider.CreateEscapedIdentifier (unescaped);
		}
		
		protected void BlankLine (ref bool first)
		{
			if (first)
				first = false;
			else
				GenerationEnvironment.AppendLine ();
		}
	}
}