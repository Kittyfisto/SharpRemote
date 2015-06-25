using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace SampleBrowser.Controls
{
	public class Console
		: Control
	{
		public static readonly DependencyProperty OutputProperty =
			DependencyProperty.Register("Output", typeof (IEnumerable<string>), typeof (Console),
			                            new PropertyMetadata(default(IEnumerable<string>)));

		static Console()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof (Console), new FrameworkPropertyMetadata(typeof (Console)));
		}

		public IEnumerable<string> Output
		{
			get { return (IEnumerable<string>) GetValue(OutputProperty); }
			set { SetValue(OutputProperty, value); }
		}
	}
}