//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Shell;
using System.Windows.Data;
using Microsoft.WindowsAPICodePack.Controls.WindowsPresentationFoundation;

namespace Microsoft.WindowsAPICodePack.Samples.KnownFoldersBrowser
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();

            Binding binding = new Binding()
                {
                    Source = knownFoldersListBox,
                    Path = new PropertyPath(ListBox.SelectedItemProperty),
                    Mode = BindingMode.OneWay,
                    TargetNullValue = ShellFileSystemFolder.FromParsingName(KnownFolders.Desktop.ParsingName)
                };

            BindingOperations.SetBinding(explorerBrowser1, ExplorerBrowser.NavigationTargetProperty, binding);
        }

        void NavigateExplorerBrowser(object sender, SelectionChangedEventArgs args)
        {
            IKnownFolder folder = (IKnownFolder)((ListBox)sender).SelectedItem;

            if (folder == null)
            {
                folder = (IKnownFolder)ShellFileSystemFolder.FromParsingName(KnownFolders.Desktop.ParsingName);
            }

            UpdateProperties(folder);
        }

        private void UpdateProperties(IKnownFolder folder)
        {
            // TODO - Make XAML only
            // There is currently no way to get all the KnownFolder properties in a collection
            // that can be use for binding to a listbox. Create our own properties collection with name/value pairs

            Collection<KnownFolderProperty> properties = new Collection<KnownFolderProperty>();
            properties.Add(new KnownFolderProperty { Property = "Canonical Name", Value = folder.CanonicalName });
            properties.Add(new KnownFolderProperty { Property = "Category", Value = folder.Category });
            properties.Add(new KnownFolderProperty { Property = "Definition Options", Value = folder.DefinitionOptions });
            properties.Add(new KnownFolderProperty { Property = "Description", Value = folder.Description });
            properties.Add(new KnownFolderProperty { Property = "File Attributes", Value = folder.FileAttributes });
            properties.Add(new KnownFolderProperty { Property = "Folder Id", Value = folder.FolderId });
            properties.Add(new KnownFolderProperty { Property = "Folder Type", Value = folder.FolderType });
            properties.Add(new KnownFolderProperty { Property = "Folder Type Id", Value = folder.FolderTypeId });
            properties.Add(new KnownFolderProperty { Property = "Localized Name", Value = folder.LocalizedName });
            properties.Add(new KnownFolderProperty { Property = "Localized Name Resource Id", Value = folder.LocalizedNameResourceId });
            properties.Add(new KnownFolderProperty { Property = "Parent Id", Value = folder.ParentId });
            properties.Add(new KnownFolderProperty { Property = "ParsingName", Value = folder.ParsingName });
            properties.Add(new KnownFolderProperty { Property = "Path", Value = folder.Path });
            properties.Add(new KnownFolderProperty { Property = "Relative Path", Value = folder.RelativePath });
            properties.Add(new KnownFolderProperty { Property = "Redirection", Value = folder.Redirection });
            properties.Add(new KnownFolderProperty { Property = "Security", Value = folder.Security });
            properties.Add(new KnownFolderProperty { Property = "Tooltip", Value = folder.Tooltip });
            properties.Add(new KnownFolderProperty { Property = "Tooltip Resource Id", Value = folder.TooltipResourceId });

            // Bind the collection to the properties listbox.
            PropertiesListBox.ItemsSource = properties;
        }
    }

    struct KnownFolderProperty
    {
        public string Property { set; get; }
        public object Value { set; get; }
    }

}
