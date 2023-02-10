using System.Collections;
using UnityEngine;

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
		public delegate void ToggledHandler(bool b);
		public event ToggledHandler Toggled;

		private readonly VUI.Size Size = new VUI.Size(500, 800);

		public delegate void Handler();
		public event Handler RightClick;

		private readonly VUI.Button button_;
		private readonly VUI.Panel panel_;
		private bool firstShow_ = true;
		private bool autoSize_ = false;
		private bool disableOverlay_ = true;

		public ToggledPanel(string buttonText, bool toolButton=false, bool autoSize=false)
		{
			if (toolButton)
				button_ = new VUI.ToolButton(buttonText, Toggle);
			else
				button_ = new VUI.Button(buttonText, Toggle);

			autoSize_ = autoSize;

			button_.Events.PointerClick += ToggleClick;

			panel_ = new VUI.Panel();
			panel_.Layout = new VUI.BorderLayout();
			panel_.BackgroundColor = VUI.Style.Theme.BackgroundColor;
			panel_.BorderColor = VUI.Style.Theme.BorderColor;
			panel_.Borders = new VUI.Insets(1);
			panel_.Clickthrough = false;
			panel_.Visible = false;

			if (!autoSize_)
				panel_.MinimumSize = Size;
		}

		public bool DisableOverlay
		{
			get { return disableOverlay_; }
			set { disableOverlay_ = value; }
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
				AlternateUI.Instance.StartCoroutine(CoDoShow());
			}
			else
			{
				DoShow();
			}
		}

		private IEnumerator CoDoShow()
		{
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			DoShow();
		}

		private void DoShow()
		{
			Toggled?.Invoke(!panel_.Visible);

			VUI.Size size;

			var root = button_.GetRoot();

			if (autoSize_)
			{
				var s = panel_.GetRealPreferredSize(
					root.FloatingPanel.Bounds.Width,
					root.FloatingPanel.Bounds.Height);

				var r = panel_.Bounds;
				r.Width = s.Width;
				r.Height = s.Height;

				panel_.SetBounds(r);
				size = r.Size;
			}
			else
			{
				size = Size;
			}

			var bounds = VUI.Rectangle.FromSize(
				root.FloatingPanel.Bounds.Left + button_.Bounds.Left,
				root.FloatingPanel.Bounds.Top + button_.Bounds.Bottom,
				size.Width, size.Height);

			if (bounds.Right >= root.FloatingPanel.Bounds.Right)
				bounds.Translate(-(bounds.Right - root.FloatingPanel.AbsoluteClientBounds.Right), 0);

			panel_.SetBounds(bounds);

			panel_.Visible = true;
			panel_.BringToTop();
			panel_.GetRoot().FocusChanged += OnFocusChanged;

			if (disableOverlay_)
			{
				panel_.GetRoot().FloatingPanel.BackgroundColor = VUI.Style.Theme.ActiveOverlayColor;
				panel_.GetRoot().FloatingPanel.Clickthrough = false;
			}

			panel_.DoLayout();

			button_.BackgroundColor = VUI.Style.Theme.HighlightBackgroundColor;
		}

		public void Hide()
		{
			panel_.Visible = false;

			panel_.GetRoot().FocusChanged -= OnFocusChanged;

			if (disableOverlay_)
			{
				panel_.GetRoot().FloatingPanel.BackgroundColor = new UnityEngine.Color(0, 0, 0, 0);
				panel_.GetRoot().FloatingPanel.Clickthrough = true;
			}

			button_.BackgroundColor = VUI.Style.Theme.ButtonBackgroundColor;
			Toggled?.Invoke(panel_.Visible);
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
