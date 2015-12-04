// 
// TypeSystemParserNode.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using Mono.Addins;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core.StringParsing;

namespace MonoDevelop.Ide.TypeSystem
{
	public class TypeSystemParserNode : TypeExtensionNode
	{
		const string ApiDefinitionBuildAction = "ObjcBindingApiDefinition";

		[NodeAttribute (Description="The build actions.")]
		string[] buildActions = { MonoDevelop.Projects.BuildAction.Compile, ApiDefinitionBuildAction };

		public string[] BuildActions {
			get {
				return buildActions;
			}
			set {
				buildActions = value;
			}
		}

		[NodeAttribute (Description="The mime type.")]
		string mimeType;

		public string MimeType {
			get {
				return mimeType;
			}
			set {
				mimeType = value;
			}
		}
		
		TypeSystemParser cachedInstance;
		
		public TypeSystemParser Parser {
			get {
				return cachedInstance ?? (cachedInstance = (TypeSystemParser)CreateInstance ());
			}
		}
		
		HashSet<string> mimeTypes;
		public bool CanParse (string mimeType, string buildAction)
		{
			if (mimeTypes == null)
				mimeTypes  = this.mimeType != null ? new HashSet<string> (this.mimeType.Split (',').Select (s => s.Trim ())) : new HashSet<string> ();
			if (!mimeTypes.Contains (mimeType, StringComparer.Ordinal))
				return false;
			return buildActions.Any (action => string.Equals (action, buildAction, StringComparison.OrdinalIgnoreCase));
		}

		public static bool IsCompileBuildAction(string buildAction)
		{
			return
				buildAction == MonoDevelop.Projects.BuildAction.Compile ||
				buildAction == ApiDefinitionBuildAction || 
				buildAction == "BMacInputs";
		}
	}


	public class TypeSystemOutputTrackingNode : ExtensionNode
	{
		[NodeAttribute (Description="The project type.")]
		string projectType;

		public string ProjectType {
			get {
				return projectType;
			}
			set {
				projectType = value;
			}
		}

		[NodeAttribute (Description="The language name.")]
		string languageName;

		public string LanguageName {
			get {
				return languageName;
			}
			set {
				languageName = value;
			}
		}
	}

}

