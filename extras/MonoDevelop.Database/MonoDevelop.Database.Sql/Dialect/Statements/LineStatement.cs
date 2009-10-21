// 
// ILineStatement.cs
//  
// Author:
//       Luciano N. Callero <lnc19@hotmail.com>
// 
// Copyright (c) 2009 
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
using System.Collections.Generic;

namespace MonoDevelop.Database.Sql
{
	public class LineStatement:IStatement
	{
		ISchemaProvider provider;
		string comment;
		string statement;
			
		public LineStatement (ISchemaProvider provider)
		{
			this.provider = provider;
		}
		
		public string Comment {
			get {return comment;}
			set {comment = value;}
		}
		
		public string Statement {
			get {return statement;}
			set {statement = value;}
		}
		
		public string GetStatement ()
		{
			System.Text.StringBuilder st = new System.Text.StringBuilder ();
			if (((AbstractSchemaProvider)provider).CanComment && comment != string.Empty) 
				st.AppendFormat ("{0} {1}", ((AbstractSchemaProvider)provider).GetComentSeparator (), comment);
			st.Append (statement);
			return st.ToString ();
			
		}
		
	}
}
