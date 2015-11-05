using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WindowsPlatform.MainToolbar
{
	/// <summary>
	/// Interaction logic for DottedSeparator.xaml
	/// </summary>
	public partial class DottedSeparator : UserControl
	{
		public DottedSeparator()
		{
			InitializeComponent();

			SizeChanged += ModifyPoints;
			dotColor = new SolidColorBrush (new Color {
				A = 0xFF,
				R = 0xB8,
				G = 0xB8,
				B = 0xB8,
			});
		}

		static Brush dotColor;
		void ModifyPoints(object sender, SizeChangedEventArgs args)
		{
			DotPanel.Children.Clear();
			
			for (int i = 0; i < args.NewSize.Height; i+=2)
			{
				DotPanel.Children.Add(new Ellipse
				{
					Fill = dotColor,
					Stroke = dotColor,
					HorizontalAlignment = HorizontalAlignment.Center,
					Width = 1,
					Height = 1,
					StrokeThickness = 1,
					Margin = new Thickness (0, 0, 0, 1),
				});
			}
		}
	}
}
