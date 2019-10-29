//
// Scaffolder.cs
//
// Author:
//       jasonimison <jaimison@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Collections.Generic;

namespace MonoDevelop.AspNetCore.Scaffolding
{
	class EmptyMvcControllerScaffolder : ControllerScaffolder
	{
		//Generator Options:
		//--controllerName|-name              : Name of the controller
		//--useAsyncActions|-async            : Switch to indicate whether to generate async controller actions
		//--noViews|-nv                       : Switch to indicate whether to generate CRUD views
		//--restWithNoViews|-api              : Specify this switch to generate a Controller with REST style API, noViews is assumed and any view related options are ignored
		//--readWriteActions|-actions         : Specify this switch to generate Controller with read/write actions when a Model class is not used
		//--model|-m                          : Model class to use
		//--dataContext|-dc                   : DbContext class to use
		//--referenceScriptLibraries|-scripts : Switch to specify whether to reference script libraries in the generated views
		//--layout|-l                         : Custom Layout page to use
		//--useDefaultLayout|-udl             : Switch to specify that default layout should be used for the views
		//--force|-f                          : Use this option to overwrite existing files
		//--relativeFolderPath|-outDir        : Specify the relative output folder path from project where the file needs to be generated, if not specified, file will be generated in the project folder
		//--controllerNamespace|-namespace    : Specify the name of the namespace to use for the generated controller


		public override string Name => "MVC Controller - Empty";

		public EmptyMvcControllerScaffolder (ScaffolderArgs args) : base (args) { }

		public override IEnumerable<ScaffolderField> Fields => stringField;
	}
}
