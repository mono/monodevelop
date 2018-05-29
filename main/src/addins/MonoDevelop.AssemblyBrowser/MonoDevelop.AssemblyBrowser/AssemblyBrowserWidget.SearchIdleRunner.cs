//
// AssemblyBrowserWidget.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using Mono.Cecil;
using MonoDevelop.Ide;
using System.Threading;
using MonoDevelop.Core.Text;
using System.Collections.Immutable;

namespace MonoDevelop.AssemblyBrowser
{
	partial class AssemblyBrowserWidget
	{
		class SearchIdleRunner
		{
			readonly AssemblyBrowserWidget assemblyBrowserWidget;
			readonly CancellationToken token;
			readonly List<IMemberDefinition> memberList = new List<IMemberDefinition> ();
			readonly bool publicOnly;
			readonly string pattern;
			readonly ImmutableList<AssemblyLoader> definitions;
			readonly SearchMode searchMode;
			readonly StringMatcher matcher;
			bool fillStepFinished;
			int currentDefinition;
			int i = 0;
			IEnumerator<TypeDefinition> currentTypeEnumerator;

			public SearchIdleRunner (AssemblyBrowserWidget assemblyBrowserWidget, string pattern, CancellationToken token)
			{
				this.assemblyBrowserWidget = assemblyBrowserWidget;
				this.publicOnly = assemblyBrowserWidget.PublicApiOnly;
				this.token = token;
				this.pattern = pattern;
				this.definitions = assemblyBrowserWidget.definitions;
				this.searchMode = assemblyBrowserWidget.searchMode;
				matcher = StringMatcher.GetMatcher (pattern, true);
			}

			public void Update ()
			{
				GLib.Idle.Add (IdleHandler);
			}

			int lastUpdateTick = Environment.TickCount;
			bool IdleHandler ()
			{
				if (token.IsCancellationRequested || (fillStepFinished && i >= memberList.Count)) {
					IdeApp.Workbench.StatusBar.EndProgress ();
					IdeApp.Workbench.StatusBar.ShowReady ();
					return false;
				}
				DoFillStep ();
				if (i < memberList.Count) {
					// only update if there are either many members or some time has passed
					if (!fillStepFinished && (memberList.Count - i > 50 || (Environment.TickCount - lastUpdateTick) > 2000))
						return true;
					lastUpdateTick = Environment.TickCount;
					assemblyBrowserWidget.searchTreeview.FreezeChildNotify ();
					for (int j = 0; j < 50 && i < memberList.Count; j++) {
						assemblyBrowserWidget.resultListStore.AppendValues (memberList [i++]);
					}
					assemblyBrowserWidget.searchTreeview.ThawChildNotify ();
				}
				return true;
			}

			void DoFillStep ()
			{
				if (fillStepFinished)
					return;
				if (currentDefinition >= definitions.Count) {
					fillStepFinished = true;
					return;
				}
				var unit = definitions [currentDefinition];
				if (currentTypeEnumerator == null) {
					currentTypeEnumerator = unit.Assembly.MainModule.Types.GetEnumerator ();
					if (!currentTypeEnumerator.MoveNext ()) {
						currentTypeEnumerator = null;
						currentDefinition++;
						return;
					}
				}
				var type = currentTypeEnumerator.Current;

				if (!currentTypeEnumerator.MoveNext ()) {
					currentTypeEnumerator = null;
					currentDefinition++;
				}

				switch (searchMode) {
				case SearchMode.Member:
					if (token.IsCancellationRequested)
						return;
					if (!type.IsPublic && publicOnly)
						return;
					foreach (var member in type.Methods) {
						if (token.IsCancellationRequested)
							return;
						if (!member.IsPublic && publicOnly)
							continue;
						if (member.IsSpecialName || member.IsRuntimeSpecialName)
							continue;
						if (matcher.IsMatch (member.Name))
							memberList.Add (member);
					}
					foreach (var member in type.Fields) {
						if (token.IsCancellationRequested)
							return;
						if (!member.IsPublic && publicOnly)
							continue;
						if (member.IsSpecialName || member.IsRuntimeSpecialName)
							continue;
						if (matcher.IsMatch (member.Name))
							memberList.Add (member);
					}
					foreach (var member in type.Properties) {
						if (token.IsCancellationRequested)
							return;
						var accessor = member.GetMethod ?? member.SetMethod;
						if (!accessor.IsPublic && publicOnly)
							continue;
						if (member.IsSpecialName || member.IsRuntimeSpecialName)
							continue;
						if (matcher.IsMatch (member.Name))
							memberList.Add (member);
					}
					foreach (var member in type.Events) {
						if (token.IsCancellationRequested)
							return;
						if (member.IsSpecialName || member.IsRuntimeSpecialName)
							continue;
						var accessor = member.AddMethod ?? member.RemoveMethod;
						if (!accessor.IsPublic && publicOnly)
							continue;
						if (matcher.IsMatch (member.Name))
							memberList.Add (member);
					}
					break;
				case SearchMode.Type:
					if (!type.IsPublic && publicOnly)
						return;
					if (type.IsSpecialName || type.IsRuntimeSpecialName || type.Name == "<Module>")
						return;
					if (matcher.IsMatch (type.FullName))
						memberList.Add (type);
					break;
				case SearchMode.TypeAndMembers:
					if (token.IsCancellationRequested)
						return;
					if (!type.IsPublic && publicOnly)
						return;
					if (type.IsSpecialName || type.IsRuntimeSpecialName || type.Name == "<Module>")
						return;
					if (matcher.IsMatch (type.FullName))
						memberList.Add (type);
					goto case SearchMode.Member;
				}
			}
		}
	}
}

