// /home/mkrueger/branch/monodevelop/Core/src/MonoDevelop.Ide/MonoDevelop.Ide.Projects/AbstractBackendBinding.cs created with MonoDelop
// User: mkrueger at 09:23 22.05.2007
//
// To change standard headers go to Edit->Preferences->Coding->Standard Headers
//

using System;
using System.Collections.Generic;

using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;

namespace MonoDevelop.Ide.Projects
{
	public abstract class AbstractBackendBinding : IBackendBinding
	{
		public virtual IParser Parser { 
			get {
				return null;
			}
		}
		
		public virtual IRefactorer Refactorer { 
			get {
				return null;
			}
		}
		
		public abstract string CommentTag {
			get;
		}
		
		bool hasProjectSupport = false;
		public bool HasProjectSupport {
			get {
				return hasProjectSupport;
			}
		}
		
		protected AbstractBackendBinding (bool hasProjectSupport)
		{
			this.hasProjectSupport = hasProjectSupport;
		}
		
		public virtual IProject LoadProject (string fileName)
		{
			return null;
		}
		
		public virtual IProject CreateProject (MonoDevelop.Projects.ProjectCreateInformation info)
		{
			return null;
		}
		
		public virtual CompilerResult Compile (List<IProject> projects, IProgressMonitor monitor)
		{
			return null;
		}
	}
}
