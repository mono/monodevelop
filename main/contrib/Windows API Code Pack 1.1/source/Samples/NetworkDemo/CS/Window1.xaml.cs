//Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Windows;
using System.Windows.Controls;
using System.Text;
using Microsoft.WindowsAPICodePack.Net;

namespace Microsoft.WindowsAPICodePack.Samples.NetworkDemo
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public Window1()
        {
            InitializeComponent();

            LoadNetworkConnections();
        }

        private void LoadNetworkConnections()
        {
            NetworkCollection networks = NetworkListManager.GetNetworks(NetworkConnectivityLevels.All);

            foreach (Network n in networks)
            {
                // Create a tab
                TabItem tabItem = new TabItem();
                tabItem.Header = string.Format("Network {0} ({1})", tabControl1.Items.Count, n.Name);
                tabControl1.Items.Add(tabItem);

                //
                StackPanel stackPanel2 = new StackPanel();
                stackPanel2.Orientation = Orientation.Vertical;

                // List all the properties
                AddProperty("Name: ", n.Name, stackPanel2);
                AddProperty("Description: ", n.Description, stackPanel2);
                AddProperty("Domain type: ", n.DomainType.ToString(), stackPanel2);
                AddProperty("Is connected: ", n.IsConnected.ToString(), stackPanel2);
                AddProperty("Is connected to the internet: ", n.IsConnectedToInternet.ToString(), stackPanel2);
                AddProperty("Network ID: ", n.NetworkId.ToString(), stackPanel2);
                AddProperty("Category: ", n.Category.ToString(), stackPanel2);
                AddProperty("Created time: ", n.CreatedTime.ToString(), stackPanel2);
                AddProperty("Connected time: ", n.ConnectedTime.ToString(), stackPanel2);
                AddProperty("Connectivity: ", n.Connectivity.ToString(), stackPanel2);

                //
                StringBuilder s = new StringBuilder();
                s.AppendLine("Network Connections:");
                NetworkConnectionCollection connections = n.Connections;
                foreach (NetworkConnection nc in connections)
                {
                    s.AppendFormat("\n\tConnection ID: {0}\n\tDomain: {1}\n\tIs connected: {2}\n\tIs connected to internet: {3}\n",
                        nc.ConnectionId, nc.DomainType, nc.IsConnected, nc.IsConnectedToInternet);
                    s.AppendFormat("\tAdapter ID: {0}\n\tConnectivity: {1}\n",
                        nc.AdapterId, nc.Connectivity);
                }
                s.AppendLine();

                Label label = new Label();
                label.Content = s.ToString();

                stackPanel2.Children.Add(label);
                tabItem.Content = stackPanel2;
            }

        }

        private void AddProperty(string propertyName, string propertyValue, StackPanel parent)
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;

            Label propertyNameLabel = new Label();
            propertyNameLabel.Content = propertyName;
            panel.Children.Add(propertyNameLabel); 

            Label propertyValueLabel = new Label();
            propertyValueLabel.Content = propertyValue;
            panel.Children.Add(propertyValueLabel);

            parent.Children.Add(panel);
        }
    }
}
