//
// BackendBindingService.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Mono.Addins;

namespace MonoDevelop.Ide.Projects
{
	public static class BackendBindingService
	{
		const string addinPath = "/MonoDevelop/BackendBindings";
		static List<BackendBindingCodon> backendBindingCodons = new List<BackendBindingCodon> ();
		
		public static ReadOnlyCollection<BackendBindingCodon> BackendBindingCodons {
			get {
				return backendBindingCodons.AsReadOnly ();
			}
		}
		
		static BackendBindingService ()
		{
			AddinManager.AddExtensionNodeHandler (addinPath, OnExtensionChanged);
			foreach (ExtensionNode node in AddinManager.GetExtensionNodes (addinPath))
				AddNode (node);
		}
		
		static void OnExtensionChanged (object sender, ExtensionNodeEventArgs e)
		{
			switch (e.Change) {
			case ExtensionChange.Add:
				AddNode (e.ExtensionNode);
				break;
			}
		}
		
		static void AddNode (ExtensionNode node)
		{
			BackendBindingCodon backendBindingCodon = node as BackendBindingCodon;
			if (backendBindingCodon != null) {
				backendBindingCodons.Add (backendBindingCodon);
			}
		}
		
		public static BackendBindingCodon GetBackendBindingCodonByGuid (string guid)
		{
			foreach (BackendBindingCodon codon in backendBindingCodons) { 
				if (codon.Guid == guid) 
					return codon;
			}
			return null;
		}
		
		public static BackendBindingCodon GetBackendBindingCodonByLanguage (string language)
		{
			foreach (BackendBindingCodon codon in backendBindingCodons) { 
				if (codon.Id == language) 
					return codon;
			}
			return null;
		}
		
		public static BackendBindingCodon GetBackendBindingCodon (SolutionProject project)
		{
			return project != null ? GetBackendBindingCodonByGuid (project.TypeGuid) : null;
		}
		
		public static IBackendBinding GetBackendBindingByGuid (string guid)
		{
			BackendBindingCodon codon = GetBackendBindingCodonByGuid (guid);
			return codon != null ? codon.BackendBinding : null;
		}
		
		public static IBackendBinding GetBackendBindingByLanguage (string language)
		{
			BackendBindingCodon codon = GetBackendBindingCodonByLanguage (language);
			return codon != null ? codon.BackendBinding : null;
		}
		
		public static IBackendBinding GetBackendBinding (SolutionProject project)
		{
			return project != null ? GetBackendBindingByGuid (project.TypeGuid) : null;
		}
		
		public static IBackendBinding GetBackendBinding (IProject project)
		{
			return GetBackendBindingByLanguage (project.Language);
		}
		
	}
}
