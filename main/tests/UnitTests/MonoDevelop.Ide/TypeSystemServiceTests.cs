//
// TypeSystemServiceTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using NUnit.Framework;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor.Projection;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.CSharpBinding;
using UnitTests;
using MonoDevelop.CSharpBinding.Tests;
using System.Linq;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.CodeCompletion;
using System.Threading.Tasks;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using ICSharpCode.NRefactory.CSharp;
using Gtk;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Ide
{
	[TestFixture]
	class TypeSystemServiceTests : TestBase
	{
		class TrackTestProject : DotNetProject
		{
			readonly string type;
			protected override void OnGetTypeTags (HashSet<string> types)
			{
				types.Add (type);
			}

			protected override DotNetCompilerParameters OnCreateCompilationParameters (DotNetProjectConfiguration config, ConfigurationKind kind)
			{
				throw new NotImplementedException ();
			}

			protected override ClrVersion [] OnGetSupportedClrVersions ()
			{
				throw new NotImplementedException ();
			}

			public TrackTestProject (string language, string type) : base(language)
			{
				this.type = type;
				Initialize (this);
			}

		}

		[Test]
		public void TestOuptutTracking_ProjectType ()
		{
			TypeSystemService.AddOutputTrackingNode (new TypeSystemOutputTrackingNode { ProjectType = "TestProjectType" });

			Assert.IsFalse (TypeSystemService.IsOutputTrackedProject (new TrackTestProject ("C#", "Bar")));
			Assert.IsTrue (TypeSystemService.IsOutputTrackedProject (new TrackTestProject ("C#", "TestProjectType")));
		}

		[Test]
		public void TestOuptutTracking_LanguageName ()
		{
			TypeSystemService.AddOutputTrackingNode (new TypeSystemOutputTrackingNode { LanguageName = "IL" });

			Assert.IsTrue (TypeSystemService.IsOutputTrackedProject (new TrackTestProject ("IL", "Bar")));
		}

	}
}
