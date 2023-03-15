using UnityEngine;

namespace AUI.MorphUI
{
	class MorphPanel : VUI.Panel
	{
		private DAZMorph morph_ = null;

		private VUI.Label name_ = new VUI.Label();
		private VUI.FloatTextSlider slider_ = new VUI.FloatTextSlider();
		private VUI.ToolButton reset_;
		private VUI.ToolButton def_;
		private VUI.ToolButton addRange_;
		private VUI.ToolButton resetRange_;
		private VUI.CheckBox fav_;

		private bool ignore_ = false;

		public MorphPanel()
		{
			int buttonsFontSize = 18;

			reset_ = new VUI.ToolButton("R", OnReset, "Reset range and value");
			def_ = new VUI.ToolButton("Def", OnDefault, "Set default value");
			addRange_ = new VUI.ToolButton("+Range", OnAddRange, "Double range");
			resetRange_ = new VUI.ToolButton("R range", OnResetRange, "Reset range");
			fav_ = new VUI.CheckBox("F", OnFavorite, false, "Favorite");

			name_.WrapMode = VUI.Label.ClipEllipsis;
			name_.FontSize = 24;
			name_.MinimumSize = new VUI.Size(DontCare, 60);
			name_.MaximumSize = new VUI.Size(DontCare, 60);

			slider_.Decimals = 3;
			slider_.TickValue = 0.01f;

			Padding = new VUI.Insets(5);
			Borders = new VUI.Insets(1);
			Layout = new VUI.VerticalFlow(
				5, true, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter);

			var buttons = new VUI.Panel(new VUI.HorizontalFlow(
				3, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter));
			buttons.Add(resetRange_);
			buttons.Add(def_);
			buttons.Add(reset_);
			buttons.Add(addRange_);
			buttons.Add(fav_);

			reset_.FontSize = buttonsFontSize;
			def_.FontSize = buttonsFontSize;
			addRange_.FontSize = buttonsFontSize;
			resetRange_.FontSize = buttonsFontSize;

			Add(name_);
			Add(slider_);
			Add(buttons);

			slider_.ValueChanged += OnValue;
		}

		public DAZMorph Morph
		{
			get { return morph_; }
		}

		public void SetMorph(DAZMorph m)
		{
			morph_ = m;

			if (morph_ == null)
			{
				Render = false;
			}
			else
			{
				Render = true;
				name_.Text = m.displayName;
				name_.Tooltip.TextFunc = GetTooltip;
				name_.Tooltip.FontSize = 24;
			}

			Update();
		}

		private string GetTooltip()
		{
			string s = $"{morph_.displayName}";

			s += $"\n";

			if (morph_.isInPackage)
				s += $"Package: {morph_.packageUid}\n";

			s +=
				$"Path: {Filter.GetPath(morph_)}\n" +
				$"Region: {morph_.region}\n" +
				$"Override region: {morph_.overrideRegion}\n" +
				$"Resolved region: {morph_.resolvedRegionName}\n" +
				$"Resolved name: {morph_.resolvedDisplayName}\n" +
				$"Name: {morph_.morphName}\n" +
				$"Group: {morph_.group}\n" +
				$"Latest: {morph_.isLatestVersion}\n" +
				$"Value: {morph_.morphValue:0.00} [{morph_.min:0.00}, {morph_.max:0.00}] Def: {morph_.startValue}";

			return s;
		}

		public void Update()
		{
			if (morph_ == null) return;

			try
			{
				ignore_ = true;
				slider_.Set(morph_.morphValue, morph_.min, morph_.max);
				fav_.Checked = morph_.favorite;

				bool active = (morph_.morphValue != morph_.startValue);

				if (active)
				{
					BackgroundColor = new Color(0.12f, 0.12f, 0.20f);
					BorderColor = new Color(1, 1, 1);
				}
				else
				{
					BackgroundColor = new Color(0, 0, 0, 0);
					BorderColor = VUI.Style.Theme.BorderColor;
				}
			}
			finally
			{
				ignore_ = false;
			}
		}

		private void OnValue(float f)
		{
			if (ignore_ || morph_ == null) return;
			morph_.morphValue = f;
		}

		private void OnReset()
		{
			if (ignore_ || morph_ == null) return;
			morph_.ResetRange();
			slider_.Set(morph_.startValue, morph_.min, morph_.max);
		}

		private void OnDefault()
		{
			if (ignore_ || morph_ == null) return;
			slider_.Value = morph_.startValue;
		}

		private void OnAddRange()
		{
			if (ignore_ || morph_ == null) return;
			morph_.min -= 1;
			morph_.max += 1;
			slider_.SetRange(morph_.min, morph_.max);
		}

		private void OnResetRange()
		{
			if (ignore_ || morph_ == null) return;
			morph_.ResetRange();
			slider_.SetRange(morph_.min, morph_.max);
		}

		private void OnFavorite(bool b)
		{
			if (ignore_ || morph_ == null) return;
			morph_.favorite = b;
		}
	}
}
