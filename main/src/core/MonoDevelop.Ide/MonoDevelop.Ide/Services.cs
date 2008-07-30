// Services.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
//
//


using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Documentation;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Gui;
using MonoDevelop.Projects.Documentation;

namespace MonoDevelop.Ide
{
	internal class Services
	{
		static IconService icons;
		static IDocumentationService documentationService;
		static TaskService taskService;
		
		public static ResourceService Resources {
			get { return MonoDevelop.Core.Gui.Services.Resources; }
		}
	
		public static IconService Icons {
			get {
				if (icons == null)
					icons = (IconService) ServiceManager.GetService (typeof(IconService));
				return icons;
			}
		}

		public static IDocumentationService DocumentationService {
			get {
				if (documentationService == null)
					documentationService = (IDocumentationService) ServiceManager.GetService (typeof(IDocumentationService));
				return documentationService;
			}
		}
	
		public static TaskService TaskService {
			get {
				if (taskService == null)
					taskService = new TaskService ();
				return taskService;
			}
		}
	
		public static IParserService ParserService {
			get { return MonoDevelop.Projects.Services.ParserService; }
		}
	
		public static ProjectService ProjectService {
			get { return MonoDevelop.Projects.Services.ProjectService; }
		}
	}
}

