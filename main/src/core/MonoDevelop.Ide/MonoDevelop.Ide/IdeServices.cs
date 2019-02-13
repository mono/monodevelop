//
// IdeApp.cs
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

using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Templates;
using System.Threading.Tasks;
using MonoDevelop.Ide.RoslynServices;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.TextEditing;
using MonoDevelop.Ide.Navigation;

namespace MonoDevelop.Ide
{
	public class IdeServices
	{
		TaskStore taskStore;
		TextEditorService textEditorService;
		NavigationHistoryService navigationHistoryManager;
		static DisplayBindingService displayBindingService;

		internal IdeServices ()
		{
		}

		internal async Task Initialize ()
		{
			taskStore = await Runtime.GetService<TaskStore> ();
			textEditorService = await Runtime.GetService<TextEditorService> ();
			navigationHistoryManager = await Runtime.GetService<NavigationHistoryService> ();
			displayBindingService = await Runtime.GetService<DisplayBindingService> ();
		}

		public TaskStore TaskStore => taskStore;

		public TextEditorService TextEditorService => textEditorService;

		public NavigationHistoryService NavigationHistory => navigationHistoryManager;

		public DisplayBindingService DisplayBindingService => displayBindingService;

		readonly Lazy<TemplatingService> templatingService = new Lazy<TemplatingService> (() => new TemplatingService ());

		public ProjectService ProjectService {
			get { return MonoDevelop.Projects.Services.ProjectService; }
		}

		public TemplatingService TemplatingService {
			get { return templatingService.Value; }
		}

		internal RoslynService RoslynService { get; } = new RoslynService ();
	}
}
