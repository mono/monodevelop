//
// FindImplementingMembersHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.Ide;
using MonoDevelop.Ide.FindInFiles;
using Microsoft.CodeAnalysis;
using MonoDevelop.Components.Commands;
using MonoDevelop.Refactoring;
using System.Linq;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading.Tasks;

namespace MonoDevelop.CSharp.Navigation
{
	class FindImplementingMembersHandler : CommandHandler
	{
		protected override async void Update (CommandInfo info)
		{
			var sym = await GetNamedTypeAtCaret (IdeApp.Workbench.ActiveDocument);
			info.Enabled = sym != null;
			info.Bypass = !info.Enabled;
		}

		protected async override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var sym = await GetNamedTypeAtCaret (doc);
			if (sym != null)
				FindImplementingSymbols (await doc.GetCompilationAsync (), sym);
		}

		static async Task<RefactoringSymbolInfo> GetNamedTypeAtCaret (Ide.Gui.Document doc)
		{
			if (doc == null)
				return null;
			var info = await RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor);

			if (info.Node?.Parent.IsKind (SyntaxKind.SimpleBaseType) != true)
				return null;
			    
			return info;
		}

		void FindImplementingSymbols (Compilation compilation, RefactoringSymbolInfo info)
		{
			var interfaceType = info.Symbol as ITypeSymbol;
			if (interfaceType == null)
				return;

			Task.Run (delegate {
				using (var monitor = IdeApp.Workbench.ProgressMonitors.GetSearchProgressMonitor (true, true)) {
					var parentTypeNode = info.Node?.Parent?.Parent?.Parent;
					if (parentTypeNode == null)
						return;
					var implementingType = info.Model.GetDeclaredSymbol (parentTypeNode) as INamedTypeSymbol;
					if (implementingType == null)
						return;
					foreach (var interfaceMember in interfaceType.GetMembers ()) {
						var impl = implementingType.FindImplementationForInterfaceMember (interfaceMember);
						if (impl == null)
							continue;
						var loc = impl.Locations.First ();
						monitor.ReportResult (new MemberReference (impl, loc.SourceTree.FilePath, loc.SourceSpan.Start, loc.SourceSpan.Length));
					}
					foreach (var iFace in interfaceType.AllInterfaces) {
					
						foreach (var interfaceMember in iFace.GetMembers ()) {
							var impl = implementingType.FindImplementationForInterfaceMember (interfaceMember);
							if (impl == null)
								continue;
							var loc = impl.Locations.First ();
							monitor.ReportResult (new MemberReference (impl, loc.SourceTree.FilePath, loc.SourceSpan.Start, loc.SourceSpan.Length));
						}
					}
						
				}
			});
		}
	}
}