// 
// InspectorAddinNode.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.SourceEditor;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.SourceEditor.QuickTasks;
using ICSharpCode.NRefactory.CSharp;

namespace MonoDevelop.CodeIssues
{
	public class CodeIssueAddinNode : TypeExtensionNode
	{
		[NodeAttribute ("mimeType", Required=true, Description="The mime type of this action.")]
		string mimeType = null;
		public string MimeType {
			get {
				return mimeType;
			}
		}

		[NodeAttribute ("severity", Required=true, Localizable=false,  Description="The severity of this action.")]
		Severity severity;
		public Severity Severity {
			get {
				return severity;
			}
		}

		[NodeAttribute ("mark", Required=false, Localizable=false,  Description="The severity of this action.")]
		IssueMarker inspectionMark = IssueMarker.Underline;
		public IssueMarker IssueMarker {
			get {
				return inspectionMark;
			}
		}

		CodeIssueProvider inspector;
		public CodeIssueProvider Inspector {
			get {
				if (inspector == null) {
					inspector = (CodeIssueProvider)CreateInstance ();
					inspector.DefaultSeverity = severity;
					inspector.MimeType = MimeType;
					inspector.IssueMarker = IssueMarker;
				}
				return inspector;
			}
		}
		
	}
}

