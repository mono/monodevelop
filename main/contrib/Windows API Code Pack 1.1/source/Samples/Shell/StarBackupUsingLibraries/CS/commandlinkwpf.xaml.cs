//Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Input;
using System.ComponentModel;

namespace Microsoft.WindowsAPICodePack.Samples.StarBackupSample
{
    /// <summary>
    /// Implements a CommandLink button that can be used in WPF user interfaces.
    /// </summary>

    public partial class CommandLinkWPF : UserControl, INotifyPropertyChanged
    {
        public CommandLinkWPF()
        {
            this.DataContext = this;
            InitializeComponent();
            this.button.Click += new RoutedEventHandler(button_Click);
        }

        void button_Click(object sender, RoutedEventArgs e)
        {
            e.Source = this;
            if (Click != null)
                Click(sender, e);
        }

        RoutedUICommand command;

        public RoutedUICommand Command
        {
            get { return command; }
            set { command = value; }
        }

        public event RoutedEventHandler Click;

        private string link;

        public string Link
        {
            get { return link; }
            set
            {
                link = value;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Link"));
                }
            }
        }
        private string note;

        public string Note
        {
            get { return note; }
            set
            {
                note = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Note"));
                }
            }
        }
        private ImageSource icon;

        public ImageSource Icon
        {
            get { return icon; }
            set
            {
                icon = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Icon"));
                }
            }
        }

        public bool? IsCheck
        {
            get
            {
                return button.IsChecked;
            }
            set { button.IsChecked = value; }
        }


        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}