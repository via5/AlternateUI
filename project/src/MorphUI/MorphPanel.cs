﻿using System;

namespace AUI.MorphUI
{
	class MorphPanel : VUI.Panel
	{
		private DAZMorph morph_ = null;

		private VUI.Label name_ = new VUI.Label();
		private VUI.FloatTextSlider slider_ = new VUI.FloatTextSlider("0.000");
		private VUI.ToolButton reset_ = new VUI.ToolButton("R");
		private VUI.ToolButton def_ = new VUI.ToolButton("Def");
		private VUI.ToolButton addRange_ = new VUI.ToolButton("+Range");
		private VUI.ToolButton resetRange_ = new VUI.ToolButton("R range");
		private VUI.CheckBox fav_ = new VUI.CheckBox("F");

		private VUI.IgnoreFlag ignore_ = new VUI.IgnoreFlag();

		public MorphPanel()
		{
			int buttonsFontSize = 18;

			name_.WrapMode = VUI.Label.ClipEllipsis;
			name_.FontSize = 24;

			Padding = new VUI.Insets(5);
			Borders = new VUI.Insets(1);
			Layout = new VUI.VerticalFlow();

			var buttons = new VUI.Panel(new VUI.HorizontalFlow(
				3, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter));
			buttons.Add(reset_);
			buttons.Add(def_);
			buttons.Add(addRange_);
			buttons.Add(resetRange_);
			buttons.Add(fav_);

			reset_.FontSize = buttonsFontSize;
			def_.FontSize = buttonsFontSize;
			addRange_.FontSize = buttonsFontSize;
			resetRange_.FontSize = buttonsFontSize;
			fav_.FontSize = buttonsFontSize;

			Add(name_);
			Add(slider_);
			Add(buttons);

			slider_.ValueChanged += OnValue;
			reset_.Clicked += OnReset;
			def_.Clicked += OnDefault;
			addRange_.Clicked += OnAddRange;
			resetRange_.Clicked += OnResetRange;
			fav_.Changed += OnFavorite;
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
			}

			Update();
		}

		private string GetTooltip()
		{
			var s =
				$"{morph_.uid}\n\n" +
				$"{morph_.morphName}\n\n" +
				$"{morph_.numDeltas} {morph_.min} {morph_.max} {morph_.isPoseControl} {morph_.group} {morph_.region}\n\n" +
				$"{Filter.GetPath(morph_)}";


			return s;
		}

		public void Update()
		{
			if (morph_ == null) return;

			ignore_.Do(() =>
			{
				slider_.Set(morph_.morphValue, morph_.min, morph_.max);
				fav_.Checked = morph_.favorite;
			});
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
			slider_.SetRange(slider_.Minimum - 1, slider_.Maximum + 1);
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
