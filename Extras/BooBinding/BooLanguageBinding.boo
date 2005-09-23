#region license
// Copyright (c) 2005, Peter Johanson (latexer@gentoo.org)
// All rights reserved.
//
// BooBinding is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// BooBinding is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with BooBinding; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
#endregion

namespace BooBinding

import System
import System.IO
import System.Diagnostics
import System.Collections
import System.Reflection
import System.Resources
import System.Xml

import MonoDevelop.Internal.Project
import MonoDevelop.Internal.Templates
import MonoDevelop.Gui
import MonoDevelop.Services

public class BooLanguageBinding(ILanguageBinding):
	internal static LanguageName = "Boo"
	compilerServices = BooBindingCompilerServices ()
	
	public def constructor():
		MonoDevelop.Services.Runtime.ProjectService.DataContext.IncludeType (typeof(BooCompilerParameters))
	
	public Language as string:
		get:
			return LanguageName
	
	public def CanCompile(fileName as string) as bool:
		Debug.Assert(compilerServices is not null)
		return compilerServices.CanCompile(fileName)
	
	public def Compile (projectFiles as ProjectFileCollection , references as ProjectReferenceCollection , configuration as DotNetProjectConfiguration , monitor as IProgressMonitor ) as ICompilerResult:
		Debug.Assert(compilerServices is not null)
		return compilerServices.Compile (projectFiles, references, configuration, monitor)
	
	public def GenerateMakefile (project as Project, parentCombine as Combine) as void:
		// FIXME: dont abort for now
		// throw NotImplementedException ()
		return
	
	public def CreateCompilationParameters (projectOptions as XmlElement) as ICloneable:
		parameters = BooCompilerParameters ()
		return parameters
	
	public CommentTag as string:
		get:
			return "//"
