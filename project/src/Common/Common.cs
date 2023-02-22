namespace AUI
{
	class SearchBox : VUI.Panel
	{
		public delegate void Handler(string s);
		public event Handler Changed;

		private VUI.TextBox box_;
		private VUI.ToolButton clear_;

		public SearchBox(string placeholder)
		{
			Layout = new VUI.BorderLayout();

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

			Add(box_, VUI.BorderLayout.Center);
			Add(clearPanel, VUI.BorderLayout.Center);
		}

		public string Text
		{
			get
			{
				return box_?.Text ?? "";
			}

			set
			{
				if (box_ != null)
					box_.Text = value;
			}
		}

		public VUI.TextBox TextBox
		{
			get { return box_; }
		}
	}
}
