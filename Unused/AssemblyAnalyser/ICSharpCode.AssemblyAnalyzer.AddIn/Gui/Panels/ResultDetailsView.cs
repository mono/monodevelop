// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Drawing;

using Gtk;

using MonoDevelop.Gui;
using MonoDevelop.Core;
using MonoDevelop.Services;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui.Pads;

using AssemblyAnalyser = ICSharpCode.AssemblyAnalyser.AssemblyAnalyser;
using Resolution = ICSharpCode.AssemblyAnalyser.Resolution;
using ICSharpCode.AssemblyAnalyser.Rules;

namespace MonoDevelop.AssemblyAnalyser
{
	public class ResultDetailsView : Frame
	{
		Resolution  currentResolution;
		Label title = new Label ();
		Label desc = new Label ();
		Label details = new Label ();

		public ResultDetailsView()
		{
			VBox vbox = new VBox ();
			
			vbox.PackStart (title);
			vbox.PackStart (desc);
			vbox.PackStart (details);
			
			this.Add (vbox);
			this.ShowAll ();
		}
		
		/*
		void HtmlControlBeforeNavigate(object sender, OpenUriArgs e)
		{
			e.RetVal = true;
			//Console.WriteLine (" >{0}< ", e.AURI);
			if (e.AURI.StartsWith ("help://types/")) {
				string typeName = e.AURI.Substring("help://types/".Length);
				//HelpBrowser helpBrowser = (HelpBrowser) WorkbenchSingleton.Workbench.GetPad (typeof (HelpBrowser));
				//helpBrowser.ShowHelpFromType (typeName);
			} else if (e.AURI.StartsWith ("help://gotocause")) {
				GotoCurrentCause ();
			}
		}
		*/
		
		public void ClearContents ()
		{
			title.Text = "";
			desc.Text = "";
			details.Text = "";
		}
		
		void GotoCurrentCause ()
		{
			Console.WriteLine ("GotoCurrentCause");
			/*
			IParserService parserService = (IParserService) ServiceManager.GetService (typeof (IParserService));
			Position position = parserService.GetPosition (currentResolution.Item.Replace ('+', '.'));
			
			if (position != null && position.Cu != null) {
				IFileService fileService = (IFileService)MonoDevelop.Core.Services.ServiceManager.GetService (typeof (IFileService));
				fileService.JumpToFilePosition (position.Cu.FileName, Math.Max (0, position.Line - 1), Math.Max (0, position.Column - 1));
			}
			*/
		}
		
		bool CanGoto (Resolution res)
		{
			Console.WriteLine ("CanGoto");
			return false;
		/*
			IParserService parserService = (IParserService) ServiceManager.GetService (typeof (IParserService));
			Position position = parserService.GetPosition (res.Item.Replace ('+', '.'));
			return position != null && position.Cu != null;
		*/
		}
		
		public void ViewResolution (Resolution resolution)
		{
			this.currentResolution = resolution;
			
			this.title.Text = resolution.FailedRule.Description;
			/*this.Html = @"<HTML><BODY ID='bodyID' CLASS='dtBODY'>
			<DIV ID='nstext'>
			<DL>" + stringParserService.Parse(resolution.FailedRule.Description)  + @"</DL>
			<H4 CLASS='dtH4'>" + stringParserService.Parse("${res:MonoDevelop.AssemblyAnalyser.ResultDetailsView.DescriptionLabel}") + @"</H4>
			<DL>" + stringParserService.Parse(resolution.FailedRule.Details) +  @"</DL>
			<H4 CLASS='dtH4'>" + stringParserService.Parse("${res:MonoDevelop.AssemblyAnalyser.ResultDetailsView.ResolutionLabel}") + @"</H4> 
			<DL>" + stringParserService.Parse(resolution.Text, resolution.Variables) +  @"</DL>
			" + (CanGoto(resolution) ? stringParserService.Parse("<A HREF=\"help://gotocause\">${res:MonoDevelop.AssemblyAnalyser.ResultDetailsView.JumpToSourceCodeLink}</A>") : "") + @"
			</DIV></BODY></HTML>";
			*/
		}
	}
}
