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
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Wizard;
using Xwt;
using Xwt.Drawing;

namespace Scaffolder
{
    class ScaffolderTemplateSelect : WizardDialogPageBase
    {
        public ScaffolderTemplateSelect()
        {
        }

        protected override Control CreateControl()
        {
            return new MonoDevelop.Components.XwtControl(new ListBox());
        }
    }

    class ScaffolderWizard : WizardDialogController
    {
        public ScaffolderWizard(string title, IWizardDialogPage firstPage) : base(title, StockIcons.Information, null, firstPage)
        {
            var rightSideImage = new Xwt.ImageView(StockIcons.ZoomIn);

            var rightSideWidget = new Xwt.FrameBox (rightSideImage);
			rightSideWidget.BackgroundColor = MonoDevelop.Ide.Gui.Styles.Wizard.PageBackgroundColor;
        }
    }

    class ScaffolderSelect : Xwt.Window

    {
        public ScaffolderSelect()
        {
            Title = "Add New Scaffolded Item";
            Width = 600;
            Height = 500;
            Resizable = true;

            var mainBox = new VBox { Spacing = 0 };

            var icon = new Xwt.ImageView(StockIcons.Information);

            var title = new Label
            {
                Markup = $"<b>Configure Scaffolding</b>",
            };
            title.Font = title.Font.WithSize(title.Font.Size + 2);

            var assembly = this.GetType().Assembly;
            var assemblyName = assembly.GetName();

            var scopyright = (assembly.GetCustomAttribute(typeof(AssemblyCopyrightAttribute)) as AssemblyCopyrightAttribute)?.Copyright ?? "Copyright © Xamarin 2017";
            var sversion = assemblyName.Version.ToString(assemblyName.Version.Revision > 0 ? 4 : 3);

            var gitVersionInformationType = assembly.GetType(assemblyName.Name + ".GitVersionInformation");
            var gitVersionBranch = "Branch";
            var gitVersionSha = "Sha";
            var sgitversion = string.Empty;
            if (!string.IsNullOrEmpty(gitVersionBranch) && !string.IsNullOrEmpty(gitVersionSha))
                sgitversion = $"({gitVersionBranch}/{gitVersionSha})";

            Label gitVersion = null;
            if (!string.IsNullOrEmpty(sgitversion))
                gitVersion = new Label(sgitversion) { TextAlignment = Alignment.Center };

            var version = new Label($"Version {sversion}") { TextAlignment = Alignment.Center };
            var copyright = new Label(scopyright) { TextAlignment = Alignment.Center };

            mainBox.PackStart(title, marginTop: 15);
            mainBox.PackStart(version, marginTop: 10);
            mainBox.PackStart(icon);
            if (gitVersion != null)
                mainBox.PackStart(gitVersion);
            mainBox.PackStart(copyright, marginTop: 10);

            var listBox = new ListBox();
            listBox.Items.Add("MVC Controller – Empty");
            listBox.Items.Add("MVC Controller with read / write actions");
            listBox.Items.Add("API Controller – Empty");
            listBox.Items.Add("API Controller with read / write actions");
            listBox.Items.Add("API Controller with actions using Entity Framework");
            listBox.Items.Add("Razor Page");
            listBox.Items.Add("Razor Page using Entity Framework");
            listBox.Items.Add("Razor Page using Entity Framework (CRUD) ");
            listBox.Items.Add("Identity");
            listBox.Items.Add("Layout ");
            mainBox.PackStart (listBox, true);

            Padding = new WidgetSpacing(20, 20, 20, 20);
            var hbox = new HBox();
            var cancelButton = new Button("Cancel");
            var backButton = new Button("Back");
            var nextButton = new Button("Next");

            nextButton.IsDefault = true;

            hbox.PackStart(cancelButton);
            hbox.PackEnd(nextButton);
            hbox.PackEnd(backButton);
            
            mainBox.PackEnd(hbox);

            Content = mainBox;
        }
    }
}
