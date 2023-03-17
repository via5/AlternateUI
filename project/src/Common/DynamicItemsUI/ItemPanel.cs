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
		private readonly VUI.Image thumbnail_;

		private DAZDynamicItem item_ = null;
		private bool ignore_ = false;

		public ItemPanel(AtomUI parent)
		{
			parent_ = parent;

			Padding = new VUI.Insets(5);
			Layout = new VUI.BorderLayout(5);

			Clickthrough = false;
			Events.PointerClick += (e) => ToggleActive();

			var top = new VUI.Panel(new VUI.BorderLayout(5));
			active_ = top.Add(new VUI.CheckBox(
				"", (b) => ToggleActive(), false, "Active"), VUI.BorderLayout.Left);
			author_ = top.Add(new VUI.Label(), VUI.BorderLayout.Center);

			var center = new VUI.Panel(new VUI.VerticalFlow(5));
			center.Add(top);
			name_ = center.Add(new VUI.Label());

			buttons_ = new VUI.Panel(new VUI.HorizontalFlow(5));
			center.Add(buttons_);

			var right = new VUI.Panel(new VUI.HorizontalFlow(5, VUI.FlowLayout.AlignDefault, true));
			thumbnail_ = right.Add(new VUI.Image());

			Add(center, VUI.BorderLayout.Center);
			Add(right, VUI.BorderLayout.Right);

			author_.FontSize = 24;
			author_.WrapMode = VUI.Label.ClipEllipsis;
			author_.Alignment = VUI.Label.AlignLeft | VUI.Label.AlignTop;
			author_.AutoTooltip = true;

			name_.FontSize = 24;
			name_.WrapMode = VUI.Label.ClipEllipsis;
			name_.Alignment = VUI.Label.AlignLeft | VUI.Label.AlignTop;
			name_.AutoTooltip = true;

			name_.MinimumSize = new VUI.Size(DontCare, 60);
			name_.MaximumSize = new VUI.Size(DontCare, 60);
		}

		public DAZDynamicItem Item
		{
			get { return item_; }
		}

		public VUI.Button AddButton(VUI.Button b)
		{
			return buttons_.Add(b);
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
					thumbnail_.Texture = null;
					Borders = new VUI.Insets(0);
				}
				else
				{
					active_.Checked = item_.active;
					author_.Text = item_.creatorName;
					name_.Text = item_.displayName;
					Borders = new VUI.Insets(1);

					if (updateThumbnail)
					{
						thumbnail_.Texture = null;

						DAZDynamicItem forItem = item_;

						item_.GetThumbnail((Texture2D t) =>
						{
							AlternateUI.Instance.StartCoroutine(CoSetTexture(forItem, t));
						});
					}
				}

				Render = (item_ != null);
				ActiveChanged();
			}
			finally
			{
				ignore_ = false;
			}
		}

		private IEnumerator CoSetTexture(DAZDynamicItem forItem, Texture2D t)
		{
			yield return new WaitForEndOfFrame();

			if (item_ == forItem)
				thumbnail_.Texture = t;
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

		private void ActiveChanged()
		{
			bool b = (item_ != null && item_.active);

			if (b)
			{
				BackgroundColor = new Color(0.12f, 0.12f, 0.20f);
				BorderColor = new Color(1, 1, 1);
			}
			else
			{
				BackgroundColor = new Color(0, 0, 0, 0);
				BorderColor = VUI.Style.Theme.BorderColor;
			}

			DoActiveChanged(b);
		}

		protected abstract void DoActiveChanged(bool b);
	}
}
