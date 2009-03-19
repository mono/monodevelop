//
// SimpleCodeCompletionDatabase.cs
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
using MonoDevelop.Core;
using System.Collections.Generic;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.Projects.Dom.Serialization
{
	internal class SimpleCodeCompletionDatabase : SerializationCodeCompletionDatabase
	{
		string file = "_currentFile";
		
		public SimpleCodeCompletionDatabase (string file, ParserDatabase pdb): base (pdb, false)
		{
			AddFile (file);
			this.file = file;
			
			string requiredRefUri = "Assembly:";
			requiredRefUri += Runtime.SystemAssemblyService.GetAssemblyForVersion (typeof(object).Assembly.FullName, null, Services.ProjectService.DefaultTargetFramework).Location;
			AddReference (requiredRefUri);
		}
		
		public TypeUpdateInformation UpdateFromParseInfo (ICompilationUnit cu)
		{
			if (cu == null)
				return new TypeUpdateInformation ();
			// TODO dom Get tag comments
//			UpdateTagComments (cu.TagComments, file);
			List<IType> resolved;
			ProjectDomService.ResolveTypes (SourceProjectDom, cu, cu.Types, out resolved);
			TypeUpdateInformation res = UpdateTypeInformation (resolved, file);
			Flush ();
			return res;
		}
		
		public override void Read () {}
		public override void Write () {}
	}
}
