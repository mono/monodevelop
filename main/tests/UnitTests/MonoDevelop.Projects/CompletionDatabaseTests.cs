// CompletionDatabaseTests.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class CompletionDatabaseTests: TestBase
	{
		Solution solution;
		ProjectDom mainProject;
		ProjectDom lib1;
		ProjectDom lib2;
		
		public override void Setup ()
		{
			base.Setup ();
			string solFile = Util.GetSampleProject ("completion-db-test", "CompletionDbTest.sln");
			Solution solution = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			ProjectDomService.Load (solution);

			Project prj;
			prj = solution.FindProjectByName ("Library2");
			lib2 = ProjectDomService.GetProjectDom (prj);
			lib2.ForceUpdate (true);
			prj = solution.FindProjectByName ("Library1");
			lib1 = ProjectDomService.GetProjectDom (prj);
			lib1.ForceUpdate (true);
			prj = solution.FindProjectByName ("CompletionDbTest");
			mainProject = ProjectDomService.GetProjectDom (prj);
			mainProject.ForceUpdate (true);
		}

		public override void TearDown ()
		{
			ProjectDomService.Unload (solution);
			base.TearDown ();
		}

		[Test]
		public void References ()
		{
			Assert.AreEqual (3, mainProject.References.Count);
			Assert.AreEqual (3, lib1.References.Count);
			Assert.AreEqual (2, lib2.References.Count);
		}
		
		[Test]
		public void SimpleGetType ()
		{
			// Simple get
			IType type = mainProject.GetType ("CompletionDbTest.MainClass");
			Assert.IsNotNull (type);
			Assert.AreEqual ("CompletionDbTest.MainClass", type.FullName);

			// Deep search in local project
			type = mainProject.GetType ("CompletionDbTest.MainClass", true);
			Assert.IsNotNull (type);
			Assert.AreEqual ("CompletionDbTest.MainClass", type.FullName);
			
			// Non deep search
			type = mainProject.GetType ("Library2.CWidget", false);
			Assert.IsNull (type);

			// Deep search by default
			type = mainProject.GetType ("Library2.CWidget");
			Assert.IsNotNull (type);
			Assert.AreEqual ("Library2.CWidget", type.FullName);

			// Deep insensitive
			type = mainProject.GetType ("library2.cwidget", true, false);
			Assert.IsNotNull (type);
			Assert.AreEqual ("Library2.CWidget", type.FullName);

			// Case sensitive
			type = mainProject.GetType ("library2.cwidget", true, true);
			Assert.IsNull (type);

			// Not generic
			type = mainProject.GetType ("CompletionDbTest.MainClass", 1, true);
			Assert.IsNull (type);

			// System.Object
			type = mainProject.GetType ("System.Object", true, true);
			Assert.IsNotNull (type);
			Assert.AreEqual ("System.Object", type.FullName);
		}

		[Test]
		public void GetGenericType ()
		{
			List<IReturnType> args = new List<IReturnType> ();
			DomReturnType rt = new DomReturnType ("System.String");
			args.Add (rt);
			IType type = mainProject.GetType ("CompletionDbTest.SomeGeneric", args);
			Assert.IsNotNull (type);
			Assert.AreEqual ("CompletionDbTest.SomeGeneric[System.String]", type.FullName);
			Assert.AreEqual (0, type.TypeParameters.Count);

			IMethod met = FindMember (type, "Run") as IMethod;
			Assert.IsNotNull (met);
			Assert.AreEqual (1, met.Parameters.Count);
			Assert.AreEqual ("System.String", met.Parameters[0].ReturnType.FullName);
			Assert.IsNotNull (met.ReturnType);
			Assert.AreEqual ("System.String", met.ReturnType.FullName);
			
			type = mainProject.GetType ("Library2.GenericWidget");
			Assert.IsNotNull (type);
			Assert.AreEqual ("Library2.GenericWidget", type.FullName);
			Assert.AreEqual (0, type.TypeParameters.Count);
			
			type = mainProject.GetType ("Library2.GenericWidget", 1, true);
			Assert.IsNotNull (type);
			Assert.AreEqual ("Library2.GenericWidget", type.FullName);
			Assert.AreEqual (1, type.TypeParameters.Count);
			
			type = mainProject.GetType ("Library2.GenericWidget", 2, true);
			Assert.IsNotNull (type);
			Assert.AreEqual ("Library2.GenericWidget", type.FullName);
			Assert.AreEqual (2, type.TypeParameters.Count);
			
			type = mainProject.GetType ("Library2.GenericWidget", 3, true);
			Assert.IsNull (type);

			// Inner generic type
			
			type = mainProject.GetType ("Library2.Container.InnerClass1", 1, true);
			Assert.IsNotNull (type);
		}
		
		IMember FindMember (IType type, string name)
		{
			foreach (IMember mem in type.Members)
				if (mem.Name == name)
					return mem;
			return null;
		}

		[Test]
		public void GetSubclasses ()
		{
			IType type = mainProject.GetType ("Library2.CWidget", true);
			Assert.IsNotNull (type);

			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetSubclasses (type, true))
				types.Add (t.FullName);

			Assert.IsTrue (types.Contains ("Library2.CContainer"));
			Assert.IsTrue (types.Contains ("Library2.SomeContainer.CInnerWidget"));
			Assert.IsTrue (types.Contains ("Library2.CExtraContainer"));
			Assert.IsTrue (types.Contains ("Library2.SomeContainer.CExtraInnerWidget"));
			Assert.IsTrue (types.Contains ("Library1.CBin"));
			Assert.IsTrue (types.Contains ("Library1.CList"));
			Assert.IsTrue (types.Contains ("Library1.SomeContainer.CInnerWidget"));
			Assert.IsTrue (types.Contains ("Library1.SomeContainer.SomeInnerContainer.CSubInnerWidget"));
			Assert.IsTrue (types.Contains ("Library1.CExtraBin"));
			Assert.IsTrue (types.Contains ("Library1.CExtraContainerSub"));
			Assert.IsTrue (types.Contains ("Library1.CExtraContainerInnerSub"));
			Assert.IsTrue (types.Contains ("Library1.CExtraContainerInnerSub.CInnerWidget1"));
			Assert.IsTrue (types.Contains ("Library1.CExtraContainerInnerSub.CInnerWidget2"));
			Assert.IsTrue (types.Contains ("Library1.CExtraContainerInnerSub.CInnerWidget3"));
			Assert.IsTrue (types.Contains ("CompletionDbTest.CustomWidget1"));
			Assert.IsTrue (types.Contains ("CompletionDbTest.CustomWidget2"));
			Assert.AreEqual (16, types.Count);

			// No deep search
			
			type = mainProject.GetType ("Library2.CWidget", true);
			Assert.IsNotNull (type);

			types.Clear ();
			foreach (IType t in mainProject.GetSubclasses (type, false))
				types.Add (t.FullName);

			Assert.IsTrue (types.Contains ("CompletionDbTest.CustomWidget1"));
			Assert.IsTrue (types.Contains ("CompletionDbTest.CustomWidget2"));
			Assert.AreEqual (2, types.Count);

			// Interface subclassing
			
			type = mainProject.GetType ("Library2.IObject", true);
			Assert.IsNotNull (type);

			types.Clear ();
			foreach (IType t in mainProject.GetSubclasses (type, true))
				types.Add (t.FullName);

			Assert.IsTrue (types.Contains ("Library2.CExtraContainer"));
			Assert.IsTrue (types.Contains ("Library2.SomeContainer.CExtraInnerWidget"));
			Assert.IsTrue (types.Contains ("Library1.CExtraContainerInnerSub"));
			Assert.IsTrue (types.Contains ("Library1.CExtraContainerInnerSub.CInnerWidget1"));
			Assert.IsTrue (types.Contains ("Library1.CExtraContainerInnerSub.CInnerWidget2"));
			Assert.IsTrue (types.Contains ("Library1.CExtraContainerSub"));
			Assert.IsTrue (types.Contains ("Library1.ISimple"));
			Assert.IsTrue (types.Contains ("CompletionDbTest.CustomWidget1"));
			Assert.IsTrue (types.Contains ("CompletionDbTest.CustomWidget2"));
			Assert.AreEqual (9, types.Count);
		}
		
		[Test]
		public void GetFileTypes ()
		{
			string file = lib1.Project.GetAbsoluteChildPath ("MyClass.cs");
			
			List<string> types = new List<string> ();
			foreach (IType t in lib1.GetTypes (file))
				types.Add (t.FullName);
			
			Assert.IsTrue (types.Contains ("Library1.CBin"));
			Assert.IsTrue (types.Contains ("Library1.CList"));
			Assert.IsTrue (types.Contains ("Library1.SomeContainer"));
			Assert.IsTrue (types.Contains ("Library1.CExtraBin"));
			Assert.IsTrue (types.Contains ("Library1.CExtraContainerSub"));
			Assert.IsTrue (types.Contains ("Library1.CExtraContainerInnerSub"));
			Assert.IsTrue (types.Contains ("Library1.ISimple"));
			Assert.AreEqual (7, types.Count);
		}
		
		[Test]
		public void GetInheritanceTree ()
		{
			IType type = mainProject.GetType ("CompletionDbTest.CustomWidget1", false);
			
			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetInheritanceTree (type)) {
				types.Add (t.FullName);
				Console.WriteLine ("pp11: " + t.FullName);
			}
			
			Assert.IsTrue (types.Contains ("CompletionDbTest.CustomWidget1"));
			Assert.IsTrue (types.Contains ("Library1.CBin"));
			Assert.IsTrue (types.Contains ("Library1.ISimple"));
			Assert.IsTrue (types.Contains ("Library2.CWidget"));
			Assert.IsTrue (types.Contains ("Library2.IObject"));
			Assert.IsTrue (types.Contains ("System.Object"));
			Assert.AreEqual (6, types.Count);
			
			type = mainProject.GetType ("CompletionDbTest.CustomWidget2", false);
			
			types = new List<string> ();
			foreach (IType t in mainProject.GetInheritanceTree (type))
				types.Add (t.FullName);
			
			Assert.IsTrue (types.Contains ("CompletionDbTest.CustomWidget2"));
			Assert.IsTrue (types.Contains ("Library1.SomeContainer.CInnerWidget"));
			Assert.IsTrue (types.Contains ("Library2.IObject"));
			Assert.IsTrue (types.Contains ("Library2.CWidget"));
			Assert.IsTrue (types.Contains ("System.Object"));
			Assert.AreEqual (5, types.Count);
		}
		
		[Test]
		public void GetNamespaceContents ()
		{
			var types = new List<string> ();
			foreach (IMember mem in mainProject.GetNamespaceContents ("SharedNamespace1", true, true))
				types.Add (mem.FullName);
			Assert.IsTrue (types.Contains ("SharedNamespace1.A"));
			Assert.IsTrue (types.Contains ("SharedNamespace1.B"));
			Assert.IsTrue (types.Contains ("SharedNamespace1.E"));
			Assert.IsTrue (types.Contains ("SharedNamespace1.F"));
			Assert.AreEqual (4, types.Count);
			
			types = new List<string> ();
			foreach (IMember mem in mainProject.GetNamespaceContents ("SharedNamespace1", false, true))
				types.Add (mem.FullName);
			Assert.IsTrue (types.Contains ("SharedNamespace1.A"));
			Assert.IsTrue (types.Contains ("SharedNamespace1.B"));
			Assert.AreEqual (2, types.Count);
			
			types = new List<string> ();
			foreach (IMember mem in mainProject.GetNamespaceContents ("sharednamespace1", true, false))
				types.Add (mem.FullName);
			Assert.IsTrue (types.Contains ("SharedNamespace1.A"));
			Assert.IsTrue (types.Contains ("SharedNamespace1.B"));
			Assert.IsTrue (types.Contains ("SharedNamespace1.E"));
			Assert.IsTrue (types.Contains ("SharedNamespace1.F"));
			Assert.AreEqual (4, types.Count);
			
			types = new List<string> ();
			foreach (IMember mem in mainProject.GetNamespaceContents ("SharedNamespace2", true, true))
				types.Add (mem.FullName);
			Assert.IsTrue (types.Contains ("SharedNamespace2.C"));
			Assert.IsTrue (types.Contains ("SharedNamespace2.D"));
			Assert.IsTrue (types.Contains ("SharedNamespace2.G"));
			Assert.IsTrue (types.Contains ("SharedNamespace2.H"));
			Assert.AreEqual (4, types.Count);
			
			types = new List<string> ();
			foreach (IMember mem in mainProject.GetNamespaceContents ("SharedNamespace2", false, true))
				types.Add (mem.FullName);
			Assert.AreEqual (0, types.Count);
		}

		[Test]
		public void GetGenericSubclassesNoParams ()
		{
			IType type = mainProject.GetType ("Library2.GenericWidget", true);
			Assert.IsNotNull (type);

			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetSubclasses (type, true))
				types.Add (GetName(t));

			Assert.IsTrue (types.Contains ("Library2.GenericBin"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericBin"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericWidget"));
			Assert.AreEqual (3, types.Count);
		}

		[Test]
		public void GetGenericSubclassesTemplate ()
		{
			// Uninstantiated generic type with one parameter
			
			IType type = mainProject.GetType ("Library2.GenericWidget", 1, true);
			Assert.IsNotNull (type);

			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetSubclasses (type, true))
				types.Add (GetName (t));

			// We don't support getting the subclasses of a parametrized type.
			// It is not clear what should be considered a subclass in this case.
			Assert.AreEqual (0, types.Count);
		}
		
		[Test]
		public void GetGenericSubclassesParamsInt ()
		{
			// Generic type with one parameter

			List<IReturnType> args = new List<IReturnType> ();
			args.Add (new DomReturnType ("System.Int32"));

			IType type = mainProject.GetType ("Library2.GenericWidget", args);
			Assert.IsNotNull (type);

			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetSubclasses (type, true))
				types.Add (GetName(t));

			Assert.IsTrue (types.Contains ("Library2.GenericBin[System.Int32]"));
			Assert.IsTrue (types.Contains ("Library2.GenericBinInt"));
			Assert.IsTrue (types.Contains ("Library2.Container.InnerClass1[System.Int32]"));
			Assert.IsTrue (types.Contains ("Library2.Container.InnerClass2"));
			Assert.IsTrue (types.Contains ("Library2.Container.InnerClass3"));
			Assert.IsTrue (types.Contains ("Library2.Container.InnerClass4"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericWidget[System.Int32]"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericBin[System.Int32]"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericBinInt1"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericBinInt2"));
			Assert.IsTrue (types.Contains ("Library1.SubInnerClass[System.Int32]"));
			Assert.IsTrue (types.Contains ("Library1.SubContainer.InnerClass1[System.Int32]"));
			Assert.IsTrue (types.Contains ("Library1.SubContainer.InnerClass2"));
			Assert.IsTrue (types.Contains ("Library1.SubContainer.InnerClass3"));
			Assert.IsTrue (types.Contains ("Library1.SubContainer.InnerClass4"));
			Assert.AreEqual (15, types.Count);
		}

		[Test]
		public void GetGenericSubclassesParamsString ()
		{
			// Generic type with one string parameter

			List<IReturnType> args = new List<IReturnType> ();
			args.Add (new DomReturnType ("System.String"));

			IType type = mainProject.GetType ("Library2.GenericWidget", args);
			Assert.IsNotNull (type);

			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetSubclasses (type, true))
				types.Add (GetName(t));

			Assert.IsTrue (types.Contains ("Library2.GenericBin[System.String]"));
			Assert.IsTrue (types.Contains ("Library2.GenericBinString"));
			Assert.IsTrue (types.Contains ("Library2.Container.InnerClass1[System.String]"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericWidget[System.String]"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericBin[System.String]"));
			Assert.IsTrue (types.Contains ("Library1.SubInnerClass[System.String]"));
			Assert.IsTrue (types.Contains ("Library1.SubContainer.InnerClass1[System.String]"));
			Assert.AreEqual (7, types.Count);
		}

		[Test]
		public void GetGenericSubclassesParamsStringInt ()
		{
			// Generic type with one string and one int

			List<IReturnType> args = new List<IReturnType> ();
			args.Add (new DomReturnType ("System.String"));
			args.Add (new DomReturnType ("System.Int32"));

			IType type = mainProject.GetType ("Library2.GenericWidget", args);
			Assert.IsNotNull (type);

			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetSubclasses (type, true))
				types.Add (GetName(t));

			Assert.IsTrue (types.Contains ("Library2.GenericBin[System.String,System.Int32]"));
			Assert.IsTrue (types.Contains ("Library2.SpecialGenericBin[System.Int32]"));
			Assert.IsTrue (types.Contains ("Library2.GenericBinStringInt"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericBin[System.String,System.Int32]"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericWidget[System.String,System.Int32]"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericBinStringInt1"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericBinStringInt2"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericWidgetStringNull[System.Int32]"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericWidgetNullInt[System.String]"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericWidgetSwapped[System.Int32,System.String]"));
			Assert.AreEqual (10, types.Count);
		}

		[Test]
		public void GetGenericSubclassesParamsIntString ()
		{
			// Generic type with one int and one string

			List<IReturnType> args = new List<IReturnType> ();
			args.Add (new DomReturnType ("System.Int32"));
			args.Add (new DomReturnType ("System.String"));

			IType type = mainProject.GetType ("Library2.GenericWidget", args);
			Assert.IsNotNull (type);

			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetSubclasses (type, true))
				types.Add (GetName(t));

			Assert.IsTrue (types.Contains ("Library2.GenericBin[System.Int32,System.String]"));
			Assert.IsTrue (types.Contains ("Library2.GenericBinIntString"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericBin[System.Int32,System.String]"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericWidget[System.Int32,System.String]"));
			Assert.IsTrue (types.Contains ("Library1.SubGenericWidgetSwapped[System.String,System.Int32]"));
			Assert.AreEqual (5, types.Count);
		}

		string GetName (IType t)
		{
			if (t.TypeParameters.Count == 0)
				return t.FullName;
			else
				return t.FullName + "`" + t.TypeParameters.Count;
		}
	}
}
