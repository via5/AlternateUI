using System.Collections;
using UnityEngine;

namespace AUI.DynamicItemsUI
{
	abstract class ItemPanel : VUI.Panel
	{
		private readonly AtomUI parent_;
		private readonly VUI.CheckBox active_;
		private readonly VUI.Label author_;
		private readonly VUI.Label name_;
		private readonly VUI.Panel buttons_;
		private readonly VUI.Button customize_ = null;
		private readonly ThumbnailPanel thumb_;

		private DAZDynamicItem item_ = null;
		private bool ignore_ = false;

		public ItemPanel(AtomUI parent)
		{
			parent_ = parent;

			Padding = new VUI.Insets(5);
			Layout = new VUI.BorderLayout(5);

			Clickthrough = false;
			Events.PointerClick += (e) =>
			{
				if (e.Button == VUI.PointerEvent.LeftButton)
					ToggleActive();
			};

			var top = new VUI.Panel(new VUI.BorderLayout(5));
			active_ = top.Add(new VUI.CheckBox(
				"", (b) => ToggleActive(), false, "Active"), VUI.BorderLayout.Left);
			author_ = top.Add(new VUI.Label(), VUI.BorderLayout.Center);

			var center = new VUI.Panel(new VUI.VerticalFlow(5));
			center.Add(top);
			name_ = center.Add(new VUI.Label());

			buttons_ = new VUI.Panel(new VUI.HorizontalFlow(5));
			customize_ = AddWidget(new VUI.ToolButton("...", OpenCustomize, "Customize"));
			center.Add(buttons_);

			var right = new VUI.Panel(new VUI.HorizontalFlow(5, VUI.FlowLayout.AlignDefault, true));
			thumb_ = new ThumbnailPanel(0, 0);
			thumb_.Tooltip.TextFunc = () => parent.MakeTooltip(item_); ;
			thumb_.Tooltip.FontSize = parent.FontSize;
			right.Add(thumb_);


			Add(center, VUI.BorderLayout.Center);
			Add(right, VUI.BorderLayout.Right);

			author_.FontSize = parent.FontSize;
			author_.WrapMode = VUI.Label.ClipEllipsis;
			author_.Alignment = VUI.Align.TopLeft;
			author_.AutoTooltip = true;

			name_.FontSize = parent.FontSize;
			name_.WrapMode = VUI.Label.ClipEllipsis;
			name_.Alignment = VUI.Align.TopLeft;
			name_.AutoTooltip = true;

			name_.MinimumSize = new VUI.Size(DontCare, 60);
			name_.MaximumSize = new VUI.Size(DontCare, 60);
		}

		public DAZDynamicItem Item
		{
			get { return item_; }
		}

		public T AddWidget<T>(T w) where T : VUI.Widget
		{
			return buttons_.Add(w);
		}

		public void Set(DAZDynamicItem item)
		{
			item_ = item;
			Update();
		}

		public void Clear()
		{
			item_ = null;
			Update();
		}

		public void Update(bool updateThumbnail = true)
		{
			try
			{
				ignore_ = true;

				if (item_ == null)
				{
					active_.Checked = false;
					author_.Text = "";
					name_.Text = "";
					thumb_.Clear();
					Borders = new VUI.Insets(0);
				}
				else
				{
					active_.Checked = item_.active;
					author_.Text = item_.creatorName;
					name_.Text = item_.displayName;
					Borders = new VUI.Insets(1);

					if (updateThumbnail)
						thumb_.Set(item_);
				}

				Render = (item_ != null);
				ActiveChanged();
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void ToggleActive()
		{
			if (ignore_) return;

			try
			{
				ignore_ = true;
				parent_.SetActive(item_, !item_.active);
				active_.Checked = item_.active;
				ActiveChanged();
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void OpenCustomize()
		{
			item_?.OpenUI();
		}

		private void ActiveChanged()
		{
			bool b = (item_ != null && item_.active);

			if (b)
			{
				BackgroundColor = new Color(0.15f, 0.15f, 0.45f);
				BorderColor = new Color(1, 1, 1);
			}
			else
			{
				BackgroundColor = new Color(0, 0, 0, 0);
				BorderColor = VUI.Style.Theme.BorderColor;
			}

			customize_.Enabled = b;

			DoActiveChanged(b);
		}

		protected abstract void DoActiveChanged(bool b);
	}
}
