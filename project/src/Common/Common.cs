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


	class ToggledPanel
	{
		private readonly VUI.Size Size = new VUI.Size(500, 800);

		public delegate void Handler();
		public event Handler RightClick;

		private readonly VUI.Button button_;
		private readonly VUI.Panel panel_;
		private bool firstShow_ = true;

		public ToggledPanel(string buttonText)
		{
			button_ = new VUI.Button(buttonText, Toggle);
			button_.Events.PointerClick += ToggleClick;

			panel_ = new VUI.Panel();
			panel_.Layout = new VUI.BorderLayout();
			panel_.BackgroundColor = VUI.Style.Theme.BackgroundColor;
			panel_.BorderColor = VUI.Style.Theme.BorderColor;
			panel_.Borders = new VUI.Insets(1);
			panel_.Clickthrough = false;
			panel_.MinimumSize = Size;
			panel_.Visible = false;
		}

		public VUI.Button Button
		{
			get { return button_; }
		}

		public VUI.Panel Panel
		{
			get { return panel_; }
		}

		public void Toggle()
		{
			if (panel_.Visible)
				Hide();
			else
				Show();
		}

		public void Show()
		{
			if (firstShow_)
			{
				firstShow_ = false;

				var root = button_.GetRoot();

				root.FloatingPanel.Add(panel_);

				panel_.SetBounds(VUI.Rectangle.FromSize(
					root.FloatingPanel.RelativeBounds.Left + button_.Bounds.Right - Size.Width,
					root.FloatingPanel.RelativeBounds.Top + button_.Bounds.Bottom,
					Size.Width, Size.Height));
			}

			panel_.Visible = true;
			panel_.BringToTop();
			panel_.GetRoot().FocusChanged += OnFocusChanged;
			panel_.GetRoot().FloatingPanel.Clickthrough = false;
			panel_.GetRoot().FloatingPanel.BackgroundColor = VUI.Style.Theme.ActiveOverlayColor;
		}

		public void Hide()
		{
			panel_.Visible = false;
			panel_.GetRoot().FocusChanged -= OnFocusChanged;
			panel_.GetRoot().FloatingPanel.Clickthrough = true;
			panel_.GetRoot().FloatingPanel.BackgroundColor = new UnityEngine.Color(0, 0, 0, 0);
		}

		private void OnFocusChanged(VUI.Widget blurred, VUI.Widget focused)
		{
			if (!focused.HasParent(panel_) && focused != button_)
				Hide();
		}

		private void ToggleClick(VUI.PointerEvent e)
		{
			if (e.Button == VUI.PointerEvent.RightButton)
				RightClick?.Invoke();

			e.Bubble = false;
		}
	}
}
