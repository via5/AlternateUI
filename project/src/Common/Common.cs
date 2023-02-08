namespace AUI
{
	class SearchBox
	{
		public delegate void Handler(string s);
		public event Handler Changed;

		private VUI.Panel panel_;
		private VUI.TextBox box_;
		private VUI.ToolButton clear_;

		public SearchBox(string placeholder)
		{
			panel_ = new VUI.Panel(new VUI.BorderLayout());

			box_ = new VUI.TextBox("", placeholder);
			box_.TextMargins = new VUI.Insets(0, 0, 44, 0);
			box_.Changed += (s) => Changed?.Invoke(s);

			var clearPanel = new VUI.Panel(new VUI.HorizontalFlow(
				0, VUI.FlowLayout.AlignRight | VUI.FlowLayout.AlignVCenter));

			clear_ = new VUI.ToolButton("X");
			clear_.Margins = new VUI.Insets(0, 2, 5, 2);
			clear_.MaximumSize = new VUI.Size(35, 35);
			clear_.Clicked += () => box_.Text = "";

			clearPanel.Add(clear_);

			panel_.Add(box_, VUI.BorderLayout.Center);
			panel_.Add(clearPanel, VUI.BorderLayout.Center);
		}

		public VUI.Widget Widget
		{
			get { return panel_; }
		}

		public string Text
		{
			get { return box_?.Text ?? ""; }
		}

		public VUI.TextBox TextBox
		{
			get { return box_; }
		}
	}
}
