//Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
{
    using System;
    using System.Windows;
    
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            WizardDialogBox wizard = new WizardDialogBox();
            bool dialogResult = (bool)wizard.ShowDialog();

            if (dialogResult == true)
            {

            }
            else
            {

            }

            // shutdown
            Application.Current.Shutdown();
        }
        
        void runWizardButton_Click(object sender, RoutedEventArgs e) 
        {
        }
    }
}