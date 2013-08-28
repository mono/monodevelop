//
// FindMemberOverloadsHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.FindInFiles;
using Mono.TextEditor;
using ICSharpCode.NRefactory.Analysis;

namespace MonoDevelop.Refactoring
{
	public class FindMemberOverloadsHandler
	{
		//Ide.Gui.Document doc;
		IMember entity;

		public FindMemberOverloadsHandler (Ide.Gui.Document doc, IMember entity)
		{
			//this.doc = doc;
			this.entity = entity;
		}

		public void Run ()
		{
			using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
				foreach (var overloadedMember in entity.DeclaringType.GetMembers (m => m.Name == entity.Name && m.SymbolKind == entity.SymbolKind)) {
					var tf = TextFileProvider.Instance.GetReadOnlyTextEditorData (overloadedMember.Region.FileName);
					var start = tf.LocationToOffset (overloadedMember.Region.Begin); 
					tf.SearchRequest.SearchPattern = overloadedMember.Name;
					var sr = tf.SearchForward (start); 
					if (sr != null)
						start = sr.Offset;
					monitor.ReportResult (new MemberReference (overloadedMember, overloadedMember.Region, start, overloadedMember.Name.Length));
				}
			}
		}
	}
}

