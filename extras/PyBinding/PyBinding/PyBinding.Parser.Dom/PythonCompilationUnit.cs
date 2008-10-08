// PythonCompilationUnit.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
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
using System.Collections.Generic;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace PyBinding.Parser.Dom
{
	[Serializable]
	public class PythonCompilationUnit : CompilationUnit
	{
		public PythonCompilationUnit (string fileName) : base (fileName)
		{
		}

		public PythonModule Module {
			get;
			set;
		}

		public void Build ()
		{
			if (Module == null)
				return;

			// fake class containing modules funcs
			var module = new DomType () {
				Name       = PythonHelper.ModuleFromFilename (FileName),
				ClassType  = ClassType.Unknown,
				Location   = new DomLocation (0, 0),
				BodyRegion = Module.Region,
			};
			Add (module);

			// module functions
			foreach (IMethod method in BuildFunctions (Module.Functions))
				module.Add (method);

			// module attributes
			foreach (IField field in BuildAttributes (Module.Attributes))
				module.Add (field);

			// module classes
			foreach (IType type in BuildClasses (Module.Classes))
				module.Add (type);
		}

		IEnumerable<IType> BuildClasses (IEnumerable<PythonClass> classes)
		{
			foreach (PythonClass pyClass in classes)
			{
				var domType = new DomType () {
					Name          = pyClass.Name,
					Documentation = pyClass.Documentation,
					ClassType     = ClassType.Class,
					BodyRegion    = pyClass.Region,
					Location      = new DomLocation (pyClass.Region.Start.Line - 1, 0),
				};
				Add (domType);

				// class functions
				foreach (IMethod method in BuildFunctions (pyClass.Functions))
					domType.Add (method);

				// class attributes
				foreach (IField field in BuildAttributes (pyClass.Attributes))
					domType.Add (field);

				yield return domType;
			}
		}

		IEnumerable<IField> BuildAttributes (IEnumerable<PythonAttribute> attributes)
		{
			foreach (PythonAttribute pyAttr in attributes)
			{
				var domAttr = new DomField () {
					Name       = pyAttr.Name,
					BodyRegion = pyAttr.Region,
					Location   = pyAttr.Region.Start,
					ReturnType = new DomReturnType { Name = pyAttr.Name },
				};
				yield return domAttr;
			}
		}

		IEnumerable<IMethod> BuildFunctions (IEnumerable<PythonFunction> functions)
		{
			if (functions == null)
				yield break;

			foreach (PythonFunction pyFunc in functions)
			{
				var domFunc = new DomMethod () {
					Name          = pyFunc.Name,
					Documentation = pyFunc.Documentation,
					BodyRegion    = pyFunc.Region,
					Location      = new DomLocation (pyFunc.Region.Start.Line - 1, 0),
					ReturnType    = new DomReturnType () { Name = String.Empty },
				};

				foreach (PythonArgument pyArg in pyFunc.Arguments)
				{
					var domArg = new DomParameter () {
						Name       = pyArg.Name,
						ReturnType = new DomReturnType () {
							Name   = pyArg.Name,
						},
					};
					domFunc.Add (domArg);
				}

				yield return domFunc;
			}
		}
	}
}