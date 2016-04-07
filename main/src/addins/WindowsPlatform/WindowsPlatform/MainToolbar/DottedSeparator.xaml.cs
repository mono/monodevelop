using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
		}

		void ModifyPoints(object sender, SizeChangedEventArgs args)
		{
			DotPanel.Children.Clear();
			
			for (int i = 0; i < args.NewSize.Height; i+=2)
			{
				DotPanel.Children.Add(new Ellipse
				{
					Fill = Foreground,
					Stroke = Foreground,
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
