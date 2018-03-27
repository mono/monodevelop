//
// AssemblyBrowserNavigationPoint.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Navigation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MonoDevelop.AssemblyBrowser
{
	class AssemblyBrowserNavigationPoint : DocumentNavigationPoint
	{
		List<AssemblyLoader> definitions;
		string idString;

		public AssemblyBrowserNavigationPoint (List<AssemblyLoader> definitions, AssemblyLoader assembly, string idString) : base (assembly?.FileName)
		{
			this.definitions = definitions;
			this.idString = idString;
		}

		protected override async Task<Document> DoShow ()
		{
			Document result = null;
			foreach (var view in Ide.IdeApp.Workbench.Documents) {
				if (view.GetContent<AssemblyBrowserViewContent> () != null) {
					view.Window.SelectWindow ();
					result = view;
					break;
				}
			}

			if (result == null) {
				var binding = DisplayBindingService.GetBindings<AssemblyBrowserDisplayBinding> ().FirstOrDefault ();
				var assemblyBrowserView = binding != null ? binding.GetViewContent () : new AssemblyBrowserViewContent ();
				assemblyBrowserView.FillWidget ();
				result = Ide.IdeApp.Workbench.OpenDocument (assemblyBrowserView, true);
			}
			if (idString != null) {
				var view = result.GetContent<AssemblyBrowserViewContent> ();
				view.Widget.suspendNavigation = true;
				view.EnsureDefinitionsLoaded (definitions);
				view.Open (idString, expandNode: false);
			} else if (FileName != null) {
				var view = result.GetContent<AssemblyBrowserViewContent> ();
				view.Widget.suspendNavigation = true;
				view.EnsureDefinitionsLoaded (definitions);
				await view.Load (FileName);
			}
			return result;
		}

		public override bool Equals (object obj)
		{
			var other = obj as AssemblyBrowserNavigationPoint;
			if (other == null)
				return false;
			if (other.idString != null)
				return other.idString.Equals (idString);
			return base.Equals (other);
		}

		public override int GetHashCode ()
		{
			return idString != null ? idString.GetHashCode () : base.GetHashCode ();
		}

		#region implemented abstract members of NavigationPoint

		public override string DisplayName {
			get {
				if (!string.IsNullOrEmpty (idString)) {
					if (!string.IsNullOrEmpty (FileName))
						return String.Format ("{0} : {1}", base.DisplayName, idString);
					return idString;
				}
				return base.DisplayName;
			}
		}

		#endregion
	}
}

