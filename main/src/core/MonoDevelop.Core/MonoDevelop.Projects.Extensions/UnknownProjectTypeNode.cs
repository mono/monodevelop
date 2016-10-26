//
// UnknownProjectTypeNode.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using MonoDevelop.Core;
using System.Linq;

namespace MonoDevelop.Projects.Extensions
{
	class UnknownProjectTypeNode: ExtensionNode
	{
		#pragma warning disable 649

		[NodeAttribute ("_instructions", Localizable=true)]
		string instructions;

		[NodeAttribute ("guid", Required = true)]
		public string Guid { get; set; }
		
		[NodeAttribute ("name", Required = true)]
		public string Name { get; set; }

		[NodeAttribute ("addin")]
		string requiresAddin { get; set; }

		[NodeAttribute ("platforms")]
		string requiresPlatform { get; set; }

		[NodeAttribute ("product")]
		string requiresProduct { get; set; }

		[NodeAttribute ("loadFiles", "If true, MonoDevelop will show the project files in the solution pad")]
		bool loadFiles = true;

		[NodeAttribute ("extension", "Extension of the project file")]
		string extension = "";

		#pragma warning restore 649

		public bool IsSolvable {
			get {
				return requiresProduct != null || requiresAddin != null;
			}
		}

		public bool LoadFiles {
			get { return loadFiles; }
		}

		public string Extension {
			get {
				return this.extension;
			}
		}

		public bool MatchesGuid (string guid)
		{
			return Guid.IndexOf (guid, StringComparison.OrdinalIgnoreCase) != -1;
		}

		public string GetInstructions ()
		{
			if (instructions != null) {
				return BrandingService.BrandApplicationName (instructions);
			}

			if (requiresPlatform != null) {
				string[] platID;
				string platName;
				if (Platform.IsMac) {
					platID = new[] { "mac" };
					platName = "OS X";
				} else if (Platform.IsWindows) {
					platID = new[] { "win32", "windows", "win" };
					platName = "Windows";
				} else {
					platID = new [] { "linux" };
					platName = "Linux";
				}
				var plats = requiresPlatform.Split (';');
				if (!plats.Any (a => platID.Any (b => string.Equals (a, b, StringComparison.OrdinalIgnoreCase)))) {
					var msg = GettextCatalog.GetString ("This project type is not supported by MonoDevelop on {0}.", platName);
					return BrandingService.BrandApplicationName (msg);
				}
			}

			if (!string.IsNullOrEmpty (requiresProduct)) {
				return GettextCatalog.GetString ("This project type requires {0} to be installed.", requiresProduct);
			}

			if (!string.IsNullOrEmpty (requiresAddin)) {
				return GettextCatalog.GetString ("The {0} extension is not installed.", requiresAddin);
			}

			if (!string.IsNullOrEmpty (instructions)) {
				return BrandingService.BrandApplicationName (Addin.Localizer.GetString (instructions));
			}

			return BrandingService.BrandApplicationLongName (GettextCatalog.GetString ("This project type is not supported by MonoDevelop."));
		}
	}
}

