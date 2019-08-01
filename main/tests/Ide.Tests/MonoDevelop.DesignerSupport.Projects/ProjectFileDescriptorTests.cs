//
// ProjectFileDescriptorTests.cs
//
// Copyright (c) 2019 Microsoft Corp
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
using MonoDevelop.Projects;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Linq;

namespace MonoDevelop.DesignerSupport.Projects
{
	[TestFixture]
	public class ProjectFileDescriptorTests : IdeTestBase
	{
		[Test]
		public void GeneratorStandardValues ()
		{
			using var project = new FileContainerProject ();
			project.Files.Add (new ProjectFile ("bar.cs") { Generator = "MyCustomTool" });
			project.Files.Add (new ProjectFile ("baz.resx") { Generator = "ResXFileCodeGenerator" });

			var file = new ProjectFile ("foo.tt");
			project.Files.Add (file);

			using var desc = new ProjectFileDescriptor (file);
			var props = desc.GetProperties ();

			var genProp = props.Find ("Generator", false);
			Assert.NotNull (genProp);

			Assert.True (genProp.Converter.GetStandardValuesSupported ());

			var ctx = new SimpleTypeDescriptorContext (null, desc);
			var standardVals = genProp.Converter.GetStandardValues (ctx);
			Assert.NotNull (standardVals);

			var standardStrings = standardVals.OfType<string> ().ToList ();
			Assert.AreEqual (standardVals.Count, standardStrings.Count);

			// resx generator is filtered to resx files so it shouldn't be here
			// even though another file in the project uses it
			Assert.False (standardStrings.Contains ("ResXFileCodeGenerator"));

			// t4 generator is registered for tt files so it should be here
			// even though it's not used on any other file in the project yet
			Assert.True (standardStrings.Contains ("TextTemplatingFileGenerator"));

			// make sure other generators seen in the project are here
			Assert.True (standardStrings.Contains ("MyCustomTool"));
		}
	}

	class FileContainerProject : Project
	{
		public FileContainerProject ()
		{
			Initialize (this);
		}
	}

	class SimpleTypeDescriptorContext : ITypeDescriptorContext
	{
		public SimpleTypeDescriptorContext (PropertyDescriptor descriptor, object instance)
		{
			PropertyDescriptor = descriptor;
			Instance = instance;
		}

		public IContainer Container => throw new NotImplementedException ();

        public object Instance { get; }

        public PropertyDescriptor PropertyDescriptor { get; }

        public object GetService (Type serviceType) => new NotImplementedException ();

		public void OnComponentChanged () => throw new NotImplementedException ();

		public bool OnComponentChanging () => throw new NotImplementedException ();
	}
}
