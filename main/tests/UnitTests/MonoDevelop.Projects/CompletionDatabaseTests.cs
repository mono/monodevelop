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
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using NUnit.Framework;
using UnitTests;
using Mono.CSharp;
using System.Linq;

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
			solution = (Solution) Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
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
		
		void ReplaceFile (string targetRelativePath, string sourceRelativePath)
		{
			string tfile = mainProject.Project.GetAbsoluteChildPath (targetRelativePath);
			string sfile = mainProject.Project.GetAbsoluteChildPath (sourceRelativePath);
			File.Copy (sfile, tfile, true);
			ProjectDomService.Parse (tfile, null);
		}

		[Test]
		public void References ()
		{
			Assert.AreEqual (4, mainProject.References.Count);
			Assert.AreEqual (3, lib1.References.Count);
			Assert.AreEqual (3, lib2.References.Count);
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
			// FIXME: deep search is currently the same as non-deep-search
//			type = mainProject.GetType ("Library2.CWidget", false);
//			Assert.IsNull (type);

			// Deep search by default
			type = mainProject.GetType ("Library2.CWidget");
			Assert.IsNotNull (type);
			Assert.AreEqual ("Library2.CWidget", type.FullName);
			
			//check that references are accessible, but not references of references
			type = mainProject.GetType ("Library3.Lib3Class");
			Assert.IsNull (type);
			type = lib2.GetType ("Library3.Lib3Class");
			Assert.IsNotNull (type);

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
			Assert.IsTrue (types.Contains ("Library1.TestAttribute"));
			
			Assert.AreEqual (8, types.Count);
		}
		
		[Test]
		public void GetInheritanceTree ()
		{
			IType type = mainProject.GetType ("CompletionDbTest.CustomWidget1", false);
			
			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetInheritanceTree (type))
				types.Add (t.FullName);
			
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
		public void GetInheritanceTreeForEnumsAndStructs ()
		{
			IType type = mainProject.GetType ("CompletionDbTest.TestEnum", false);
			
			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetInheritanceTree (type)) {
				Console.WriteLine (t.FullName);
				types.Add (t.FullName);
			}
			
			Assert.IsTrue (types.Contains ("CompletionDbTest.TestEnum"));
			Assert.IsTrue (types.Contains ("System.Enum"));
			Assert.IsTrue (types.Contains ("System.Object"));
			
			type = mainProject.GetType ("CompletionDbTest.TestStruct", false);
			
			types = new List<string> ();
			foreach (IType t in mainProject.GetInheritanceTree (type))
				types.Add (t.FullName);
			
			Assert.IsTrue (types.Contains ("CompletionDbTest.TestStruct"));
			Assert.IsTrue (types.Contains ("System.ValueType"));
			Assert.IsTrue (types.Contains ("System.Object"));
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

/*		[Test]
		public void GetObjectSubclasses ()
		{
			IType type = mainProject.GetType ("System.Object", true);
			Assert.IsNotNull (type);

			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetSubclasses (type, true))
				types.Add (t.FullName);
		}
*/
		
		[Test]
		public void RewriteGenericType ()
		{
			// Check that the instantiated type cache is properly invalidated
			// when a generic type changes.
			
			List<IReturnType> args = new List<IReturnType> ();
			args.Add (new DomReturnType ("System.Int32"));
			IType type = mainProject.GetType ("CompletionDbTest.GenericRewrite", args);
			Assert.IsNotNull (type);
			Assert.IsTrue (type is InstantiatedType);
			Assert.IsTrue (type.FieldCount == 1);
			
			ReplaceFile ("GenericRewrite.cs", "Replacements/GenericRewrite.cs");
			
			type = mainProject.GetType ("CompletionDbTest.GenericRewrite", args);
			Assert.IsNotNull (type);
			Assert.IsTrue (type is InstantiatedType);
			Assert.IsTrue (type.FieldCount == 2);
		}
		
		[Test]
		public void GenericConstraintTest_Class ()
		{
			IType type = mainProject.GetType ("CompletionDbTest.GenericConstraintTest1", 1, false);
			Assert.IsNotNull (type);
			Assert.AreEqual ("CompletionDbTest.GenericConstraintTest1", type.FullName);
			Assert.AreEqual (1, type.TypeParameters.Count);
			Assert.AreEqual (1, type.FieldCount);
			
			List<IField> fs = new List<IField> (type.Fields);
			IReturnType rt = fs [0].ReturnType;
			
			IType fieldType = mainProject.GetType (rt);
			Assert.IsNotNull (fieldType);
			Assert.AreEqual ("T", fieldType.Name);
			
			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetInheritanceTree (fieldType))
				types.Add (t.FullName);
			
			Assert.IsTrue (types.Contains ("CompletionDbTest.GenericConstraintTest1.T"));
			Assert.IsTrue (types.Contains ("System.Object"));
			Assert.AreEqual (2, types.Count);
		}
		
		[Test]
		public void GenericConstraintTest_Struct ()
		{
			IType type = mainProject.GetType ("CompletionDbTest.GenericConstraintTest2", 1, false);
			Assert.IsNotNull (type);
			Assert.AreEqual ("CompletionDbTest.GenericConstraintTest2", type.FullName);
			Assert.AreEqual (1, type.TypeParameters.Count);
			Assert.AreEqual (1, type.FieldCount);
			
			List<IField> fs = new List<IField> (type.Fields);
			IReturnType rt = fs [0].ReturnType;
			
			IType fieldType = mainProject.GetType (rt);
			Assert.IsNotNull (fieldType);
			Assert.AreEqual ("T", fieldType.Name);
			
			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetInheritanceTree (fieldType))
				types.Add (t.FullName);
			
			Assert.IsTrue (types.Contains ("CompletionDbTest.GenericConstraintTest2.T"));
			Assert.IsTrue (types.Contains ("System.ValueType"));
			Assert.IsTrue (types.Contains ("System.Object"));
			Assert.AreEqual (3, types.Count);
		}
		
		[Test]
		public void GenericConstraintTest_New ()
		{
			IType type = mainProject.GetType ("CompletionDbTest.GenericConstraintTest3", 1, false);
			Assert.IsNotNull (type);
			Assert.AreEqual ("CompletionDbTest.GenericConstraintTest3", type.FullName);
			Assert.AreEqual (1, type.TypeParameters.Count);
			Assert.AreEqual (1, type.FieldCount);
			
			List<IField> fs = new List<IField> (type.Fields);
			IReturnType rt = fs [0].ReturnType;
			
			IType fieldType = mainProject.GetType (rt);
			Assert.IsNotNull (fieldType);
			Assert.AreEqual ("T", fieldType.Name);
			
			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetInheritanceTree (fieldType))
				types.Add (t.FullName);
			
			Assert.IsTrue (types.Contains ("CompletionDbTest.GenericConstraintTest3.T"));
			Assert.IsTrue (types.Contains ("System.Object"));
			Assert.AreEqual (2, types.Count);
		}
		
		[Test]
		public void GenericConstraintTest_WithBase ()
		{
			IType type = mainProject.GetType ("CompletionDbTest.GenericConstraintTest4", 2, false);
			Assert.IsNotNull (type);
			Assert.AreEqual ("CompletionDbTest.GenericConstraintTest4", type.FullName);
			Assert.AreEqual (2, type.TypeParameters.Count);
			Assert.AreEqual (2, type.FieldCount);
			
			// First field
			
			List<IField> fs = new List<IField> (type.Fields);
			IReturnType rt = fs [0].ReturnType;
			
			IType fieldType = mainProject.GetType (rt);
			Assert.IsNotNull (fieldType);
			Assert.AreEqual ("T", fieldType.Name);
			
			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetInheritanceTree (fieldType))
				types.Add (t.FullName);
			
			Assert.IsTrue (types.Contains ("CompletionDbTest.GenericConstraintTest4.T"));
			Assert.IsTrue (types.Contains ("Library1.CBin"));
			Assert.IsTrue (types.Contains ("Library2.CWidget"));
			Assert.IsTrue (types.Contains ("System.Object"));
			Assert.AreEqual (4, types.Count);
			
			// Second field
			
			rt = fs [1].ReturnType;
			
			fieldType = mainProject.GetType (rt);
			Assert.IsNotNull (fieldType);
			Assert.AreEqual ("U", fieldType.Name);
			
			types = new List<string> ();
			foreach (IType t in mainProject.GetInheritanceTree (fieldType))
				types.Add (t.FullName);
			
			Assert.IsTrue (types.Contains ("CompletionDbTest.GenericConstraintTest4.U"));
			Assert.IsTrue (types.Contains ("CompletionDbTest.GenericConstraintTest4.T"));
			Assert.IsTrue (types.Contains ("Library1.CBin"));
			Assert.IsTrue (types.Contains ("Library2.CWidget"));
			Assert.IsTrue (types.Contains ("System.Object"));
			Assert.AreEqual (5, types.Count);
		}
		
		[Test]
		public void GenericConstraintTest_WithWrongBase ()
		{
			IType type = mainProject.GetType ("CompletionDbTest.GenericConstraintTest5", 2, false);
			Assert.IsNotNull (type);
			Assert.AreEqual ("CompletionDbTest.GenericConstraintTest5", type.FullName);
			Assert.AreEqual (2, type.TypeParameters.Count);
			Assert.AreEqual (2, type.FieldCount);
			
			// First field
			
			List<IField> fs = new List<IField> (type.Fields);
			IReturnType rt = fs [0].ReturnType;
			
			IType fieldType = mainProject.GetType (rt);
			Assert.IsNotNull (fieldType);
			Assert.AreEqual ("T", fieldType.Name);
			
			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetInheritanceTree (fieldType))
				types.Add (t.FullName);
			
			Assert.IsTrue (types.Contains ("CompletionDbTest.GenericConstraintTest5.T"));
			Assert.IsTrue (types.Contains ("System.Object"));
			Assert.AreEqual (2, types.Count);
			
			// Second field
			
			rt = fs [1].ReturnType;
			
			fieldType = mainProject.GetType (rt);
			Assert.IsNotNull (fieldType);
			Assert.AreEqual ("U", fieldType.Name);
			
			types = new List<string> ();
			foreach (IType t in mainProject.GetInheritanceTree (fieldType))
				types.Add (t.FullName);
			
			Assert.IsTrue (types.Contains ("CompletionDbTest.GenericConstraintTest5.U"));
			Assert.IsTrue (types.Contains ("System.Object"));
			Assert.AreEqual (2, types.Count);
		}		
		
		[Test]
		public void GenericConstraintTest_ClassAndInterface ()
		{
			IType type = mainProject.GetType ("CompletionDbTest.GenericConstraintTest6", 1, false);
			Assert.IsNotNull (type);
			Assert.AreEqual ("CompletionDbTest.GenericConstraintTest6", type.FullName);
			Assert.AreEqual (1, type.TypeParameters.Count);
			Assert.AreEqual (1, type.FieldCount);
			
			List<IField> fs = new List<IField> (type.Fields);
			IReturnType rt = fs [0].ReturnType;
			
			IType fieldType = mainProject.GetType (rt);
			Assert.IsNotNull (fieldType);
			Assert.AreEqual ("T", fieldType.Name);
			
			List<string> types = new List<string> ();
			foreach (IType t in mainProject.GetInheritanceTree (fieldType))
				types.Add (t.FullName);
			
			Assert.IsTrue (types.Contains ("CompletionDbTest.GenericConstraintTest6.T"));
			Assert.IsTrue (types.Contains ("Library1.CBin"));
			Assert.IsTrue (types.Contains ("Library2.CWidget"));
			Assert.IsTrue (types.Contains ("System.ICloneable"));
			Assert.IsTrue (types.Contains ("System.Object"));
			Assert.AreEqual (5, types.Count);
		}
		
		[Test]
		public void PartialClass ()
		{
			IType type = mainProject.GetType ("CompletionDbTest.PartialTest");
			Assert.IsNotNull (type);
			Assert.AreEqual ("CompletionDbTest.PartialTest", type.FullName);
			
			List<string> members = new List<string> ();
			foreach (IMember mem in type.Members)
				members.Add (mem.Name);
			
			Assert.AreEqual (15, members.Count);
			Assert.IsTrue (members.Contains ("Field1"));
			Assert.IsTrue (members.Contains ("Property1"));
			Assert.IsTrue (members.Contains ("Event1"));
			Assert.IsTrue (members.Contains ("Method1"));
			Assert.IsTrue (members.Contains ("Inner1"));
			Assert.IsTrue (members.Contains ("Field2"));
			Assert.IsTrue (members.Contains ("Property2"));
			Assert.IsTrue (members.Contains ("Event2"));
			Assert.IsTrue (members.Contains ("Method2"));
			Assert.IsTrue (members.Contains ("Inner2"));
			Assert.IsTrue (members.Contains ("Field3"));
			Assert.IsTrue (members.Contains ("Property3"));
			Assert.IsTrue (members.Contains ("Event3"));
			Assert.IsTrue (members.Contains ("Method3"));
			Assert.IsTrue (members.Contains ("Inner3"));
			
			ReplaceFile ("PartialTest2.cs", "Replacements/PartialTest2.cs");
			
			type = mainProject.GetType ("CompletionDbTest.PartialTest");
			Assert.IsNotNull (type);
			Assert.AreEqual ("CompletionDbTest.PartialTest", type.FullName);
			
			members = new List<string> ();
			foreach (IMember mem in type.Members)
				members.Add (mem.Name);
			
			Assert.AreEqual (10, members.Count);
			Assert.IsTrue (members.Contains ("Field2"));
			Assert.IsTrue (members.Contains ("Property2"));
			Assert.IsTrue (members.Contains ("Event2"));
			Assert.IsTrue (members.Contains ("Method2"));
			Assert.IsTrue (members.Contains ("Inner2"));
			Assert.IsTrue (members.Contains ("Field3"));
			Assert.IsTrue (members.Contains ("Property3"));
			Assert.IsTrue (members.Contains ("Event3"));
			Assert.IsTrue (members.Contains ("Method3"));
			Assert.IsTrue (members.Contains ("Inner3"));
			
			ReplaceFile ("PartialTest2.cs", "Replacements/EmptyFile.cs");
			
			type = mainProject.GetType ("CompletionDbTest.PartialTest");
			Assert.IsNotNull (type);
			Assert.AreEqual ("CompletionDbTest.PartialTest", type.FullName);
			
			members = new List<string> ();
			foreach (IMember mem in type.Members)
				members.Add (mem.Name);
			
			Assert.AreEqual (5, members.Count);
			Assert.IsTrue (members.Contains ("Field3"));
			Assert.IsTrue (members.Contains ("Property3"));
			Assert.IsTrue (members.Contains ("Event3"));
			Assert.IsTrue (members.Contains ("Method3"));
			Assert.IsTrue (members.Contains ("Inner3"));
			
			ReplaceFile ("PartialTest1.cs", "Replacements/EmptyFile.cs");
			
			type = mainProject.GetType ("CompletionDbTest.PartialTest");
			Assert.IsNull (type);
		}
		
		[Test]
		public void NamespaceExistsTest ()
		{
			Assert.IsTrue (mainProject.NamespaceExists ("Level1"), "Level1 doesn't exist.");
			Assert.IsTrue (mainProject.NamespaceExists ("Level1.Level2"), "Level1.Level2 doesn't exist.");
			Assert.IsTrue (mainProject.NamespaceExists ("Level1.Level2.Level3"), "Level1.Level2.Level3 doesn't exist.");
			Assert.IsTrue (mainProject.NamespaceExists ("Level1.Level2.Level3.Level4"), "Level1.Level2.Level3.Level4 doesn't exist.");
			Assert.IsFalse (mainProject.NamespaceExists ("Level1.Level2.Level3.Level4.Level5"), "Level5 shouldn't exist.");
			Assert.IsFalse (mainProject.NamespaceExists ("Level1.Level3"), "level1.level3 shouldn't exist.");
		}
		
		[Test]
		public void ClassAttributeTest ()
		{
			// Simple get
			IType type = mainProject.GetType ("CompletionDbTest.AttributeTest");
			Assert.IsNotNull (type);
			Assert.AreEqual (1, type.Attributes.Count ());
			Assert.AreEqual ("Serializable", type.Attributes.First ().Name);
		}
		
		[Test]
		public void MemberAttributeTest ()
		{
			// Simple get
			IType type = mainProject.GetType ("CompletionDbTest.AttributeTest2");
			Assert.IsNotNull (type);
			
			var prop = type.Properties.First ();
			Assert.AreEqual (1, prop.Attributes.Count ());
			Assert.AreEqual ("Obsolete", prop.Attributes.First ().Name);
			
			var method = type.Methods.First ();
			Assert.AreEqual (1, method.Attributes.Count ());
			Assert.AreEqual ("Obsolete", method.Attributes.First ().Name);
		}
		
		[Test]
		public void CustomAttributeTest ()
		{
			// Simple get
			IType type = mainProject.GetType ("CompletionDbTest.AttributeTest3");
			Assert.IsNotNull (type);
			Assert.AreEqual (1, type.Attributes.Count ());
			
			var att = type.Attributes.First ();
			Assert.AreEqual ("Library1.TestAttribute", att.AttributeType.FullName);
			Assert.AreEqual (2, att.PositionalArguments.Count);
			
			var expr1 = att.PositionalArguments[0] as System.CodeDom.CodePrimitiveExpression;
			Assert.IsNotNull (expr1);
			Assert.AreEqual ("str1", expr1.Value);
			
			var expr2 = att.PositionalArguments[1] as System.CodeDom.CodePrimitiveExpression;
			Assert.IsNotNull (expr2);
			Assert.AreEqual (5, expr2.Value);
			
			Assert.AreEqual (1, att.NamedArguments.Count);
			Assert.IsTrue (att.NamedArguments.ContainsKey ("Blah"));
			var expr3 = att.NamedArguments["Blah"] as System.CodeDom.CodePrimitiveExpression;
			Assert.IsNotNull (expr3);
			Assert.AreEqual ("str2", expr3.Value);
		}
		
	}
}
