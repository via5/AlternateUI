using System.Collections;
using UnityEngine;

namespace AUI.ClothingUI
{
	class ClothingPanel : VUI.Panel
	{
		private readonly ClothingAtomInfo parent_;
		private readonly VUI.CheckBox active_ = null;
		private readonly VUI.Label author_ = null;
		private readonly VUI.Label name_ = null;
		private readonly VUI.Button customize_ = null;
		private readonly VUI.Button adjustments_ = null;
		private readonly VUI.Button physics_ = null;
		private readonly VUI.Image thumbnail_ = null;

		private DAZClothingItem ci_ = null;
		private bool ignore_ = false;

		public ClothingPanel(ClothingAtomInfo parent)
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

			var buttons = new VUI.Panel(new VUI.HorizontalFlow(5));
			customize_ = buttons.Add(new VUI.ToolButton("...", OpenCustomize, "Customize"));
			adjustments_ = buttons.Add(new VUI.ToolButton("A", OpenAdjustments, "Adjustments"));
			physics_ = buttons.Add(new VUI.ToolButton("P", OpenPhysics, "Physics"));

			center.Add(buttons);

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

			Update();
		}

		public void Set(DAZClothingItem ci)
		{
			ci_ = ci;
			Update();
		}

		public void Clear()
		{
			ci_ = null;
			Update();
		}

		public void Update(bool updateThumbnail=true)
		{
			try
			{
				ignore_ = true;

				if (ci_ == null)
				{
					active_.Checked = false;
					author_.Text = "";
					name_.Text = "";
					thumbnail_.Texture = null;
					Borders = new VUI.Insets(0);
				}
				else
				{
					active_.Checked = ci_.active;
					author_.Text = ci_.creatorName;
					name_.Text = ci_.displayName;
					Borders = new VUI.Insets(1);

					if (updateThumbnail)
					{
						thumbnail_.Texture = null;

						DAZClothingItem forCi = ci_;

						ci_.GetThumbnail((Texture2D t) =>
						{
							AlternateUI.Instance.StartCoroutine(CoSetTexture(forCi, t));
						});
					}
				}

				Render = (ci_ != null);
				ActiveChanged();
			}
			finally
			{
				ignore_ = false;
			}
		}

		private IEnumerator CoSetTexture(DAZClothingItem forCi, Texture2D t)
		{
			yield return new WaitForEndOfFrame();

			if (ci_ == forCi)
				thumbnail_.Texture = t;
		}

		public void ToggleActive()
		{
			if (ignore_) return;

			try
			{
				ignore_ = true;
				ci_.characterSelector.SetActiveClothingItem(ci_, !ci_.active);
				active_.Checked = ci_.active;
				ActiveChanged();
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void OpenCustomize()
		{
			ClothingUI.OpenClothingUI(ci_);
		}

		public void OpenPhysics()
		{
			ClothingUI.OpenClothingUI(ci_, "Physics");
		}

		public void OpenAdjustments()
		{
			ClothingUI.OpenClothingUI(ci_, "Adjustments");
		}

		private void ActiveChanged()
		{
			bool b = (ci_ != null && ci_.active);

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

			customize_.Enabled = b;
			physics_.Enabled = b;
			adjustments_.Enabled = b;
		}
	}
}
