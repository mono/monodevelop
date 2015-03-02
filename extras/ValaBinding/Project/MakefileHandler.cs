using System;
using System.IO;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Autotools;
using MonoDevelop.Projects;
using MonoDevelop.Deployment;

namespace MonoDevelop.ValaBinding
{
	/// <summary>
	/// Handler for makefile generation
	/// </summary>
    public class MakefileHandler : IMakefileHandler
    {

        #region IMakefileHandler implementation
        public bool CanDeploy(MonoDevelop.Projects.SolutionItem entry, MakefileType type)
        {
            return entry is ValaProject;
        }

        /// <summary>
        /// Deploys a makefile to build the default configuration. 
        /// </summary>
        /// <remarks>
        /// TODO: Make configuration-based targets as advertised.
        /// </remarks>
        public Makefile Deploy(AutotoolsContext ctx, MonoDevelop.Projects.SolutionItem entry, MonoDevelop.Core.IProgressMonitor monitor)
        {
            Makefile mkfile = new Makefile();
            ValaProject project = (ValaProject)entry;
            ValaProjectConfiguration conf = (ValaProjectConfiguration)project.DefaultConfiguration;

            StringBuilder files = new StringBuilder();
            foreach (ProjectFile t in project.Files)
            {
                if (BuildAction.Compile == t.BuildAction)
                {
                    files.Append("\\\n\t" + FileService.AbsoluteToRelativePath(project.BaseDirectory, t.FilePath));
                }
            }

            string dir = ctx.DeployContext.GetResolvedPath(TargetDirectory.ProgramFiles,
                FileService.AbsoluteToRelativePath(
                    FileService.RelativeToAbsolutePath(conf.SourceDirectory, conf.OutputDirectory),
                ctx.TargetSolution.BaseDirectory));
            dir = dir.Replace("@prefix@", "$(prefix)");
            dir = dir.Replace("@PACKAGE@", "$(PACKAGE)");

            TemplateEngine templateEngine = new TemplateEngine();
            templateEngine.Variables["TOP_SRCDIR"] = FileService.AbsoluteToRelativePath(project.BaseDirectory, ctx.TargetSolution.BaseDirectory);
            templateEngine.Variables["FILES"] = files.ToString();
            templateEngine.Variables["BUILD_DIR"] = ".";
            templateEngine.Variables["INSTALL_DIR"] = "$(DESTDIR)" + dir;
            templateEngine.Variables["ALL_TARGET"] = string.Format("all-{0}", conf.Name);
            templateEngine.Variables["VFLAGS"] = string.Format("{0} {1}", ValaCompiler.GetCompilerFlags(conf),
                ValaCompiler.GeneratePkgCompilerArgs(project.Packages, conf.Selector));
            templateEngine.Variables["VTARGET"] = conf.CompiledOutputName;

            StringWriter sw = new StringWriter();

            string mt;
            if (ctx.MakefileType == MakefileType.AutotoolsMakefile)
                mt = "Makefile.am.template";
            else
                mt = "Makefile.template";

            using (Stream stream = GetType().Assembly.GetManifestResourceStream(mt))
            {
                StreamReader reader = new StreamReader(stream);

                templateEngine.Process(reader, sw);
                reader.Close();
            }

            mkfile.Append(sw.ToString());

            return mkfile;
        }
        #endregion
    }
}
