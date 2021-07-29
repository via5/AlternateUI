﻿using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	class Label : Panel
	{
		public override string TypeName { get { return "Label"; } }

		public const int AlignLeft = 0x01;
		public const int AlignCenter = 0x02;
		public const int AlignRight = 0x04;
		public const int AlignTop = 0x08;
		public const int AlignVCenter = 0x10;
		public const int AlignBottom = 0x20;

		public const int Wrap = 0;
		public const int Overflow = 1;
		public const int Clip = 2;
		public const int ClipEllipsis = 3;

		private string text_;
		private int align_;
		private Text textObject_ = null;
		private Text ellipsis_ = null;
		private int wrap_ = Overflow;
		private bool autoTooltip_ = false;

		public Label(string t = "", int align = AlignLeft | AlignVCenter)
		{
			text_ = t;
			align_ = align;
		}

		public Label(string t, FontStyle fs)
			: this(t)
		{
			FontStyle = fs;
		}

		public string Text
		{
			get
			{
				return text_;
			}

			set
			{
				if (text_ != value)
				{
					text_ = value;

					if (IsVisibleOnScreen() && TextTooLong())
						NeedsLayout($"text changed");

					if (textObject_ != null)
						textObject_.text = value;
				}
			}
		}

		public int Alignment
		{
			get
			{
				return align_;
			}

			set
			{
				align_ = value;
				NeedsLayout("alignment changed");
			}
		}

		public int WrapMode
		{
			get
			{
				return wrap_;
			}

			set
			{
				wrap_ = value;
				NeedsLayout("wrap changed");
			}
		}

		public bool AutoTooltip
		{
			get
			{
				return autoTooltip_;
			}

			set
			{
				autoTooltip_ = value;
				UpdateClip();
			}
		}

		protected override void DoCreate()
		{
			base.DoCreate();

			textObject_ = WidgetObject.AddComponent<Text>();
			textObject_.text = text_;
			textObject_.horizontalOverflow = GetHorizontalOverflow();
			textObject_.maskable = true;

			// needed for tooltips
			textObject_.raycastTarget = true;

			Style.Setup(this);
		}

		private HorizontalWrapMode GetHorizontalOverflow()
		{
			if (wrap_ == Wrap)
				return HorizontalWrapMode.Wrap;
			else
				return HorizontalWrapMode.Overflow;
		}

		protected override void DoPolish()
		{
			base.DoPolish();
			Style.Polish(this);
		}

		public override void UpdateBounds()
		{
			base.UpdateBounds();
			textObject_.alignment = ToTextAnchor(align_);
			UpdateClip();
		}

		private void UpdateClip()
		{
			if (textObject_ == null)
				return;

			switch (wrap_)
			{
				case Wrap:
				case Overflow:
				{
					textObject_.SetClipRect(Rect.zero, false);
					break;
				}

				case Clip:
				{
					var ar = AbsoluteClientBounds;
					var root = GetRoot();

					if (root != null)
					{
						var r = new Rect(
							ar.Left - root.Bounds.Width / 2,
							root.Bounds.Height - ar.Top + ar.Height,
							ar.Width, ar.Height);

						textObject_.SetClipRect(r, true);
					}

					break;
				}

				case ClipEllipsis:
				{
					if (TextTooLong())
					{
						var ar = AbsoluteClientBounds;
						var root = GetRoot();

						if (root != null)
						{
							var ellipsisSize = Root.TextSize(Font, FontSize, "...");

							var cr = new Rect(
								ar.Left - root.Bounds.Width / 2,
								root.Bounds.Height - ar.Top + ar.Height,
								ar.Width - ellipsisSize.Width - 5, ar.Height);

							textObject_.SetClipRect(cr, true);

							if (ellipsis_ == null)
								CreateEllipsis();

							var r = Rectangle.FromSize(
								RelativeBounds.Width - ellipsisSize.Width,
								RelativeBounds.Top,
								ellipsisSize.Width, ellipsisSize.Height);

							Utilities.SetRectTransform(ellipsis_, r);

							if (autoTooltip_)
								Tooltip.Text = text_;
						}
					}
					else
					{
						if (autoTooltip_)
							Tooltip.Text = "";
					}

					break;
				}
			}
		}

		private void CreateEllipsis()
		{
			var go = new GameObject("ellipsis");
			go.AddComponent<RectTransform>();
			go.AddComponent<LayoutElement>();
			ellipsis_ = go.AddComponent<Text>();
			go.SetActive(true);
			go.transform.SetParent(MainObject.transform, false);
			ellipsis_.text = "...";
			ellipsis_.raycastTarget = false;

			Polish();
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			if (wrap_ == Wrap)
			{
				return Root.FitText(
					Font, FontSize, text_, new Size(maxWidth, maxHeight));
			}
			else
			{
				return Root.FitText(
					Font, FontSize, text_, new Size(DontCare, maxHeight));
			}
		}

		protected override Size DoGetMinimumSize()
		{
			return Root.TextSize(Font, FontSize, text_) + new Size(0, 5);
		}

		private bool TextTooLong()
		{
			// todo: wrap mode
			return (Root.TextLength(Font, FontSize, text_) > Bounds.Width);
		}

		public static TextAnchor ToTextAnchor(int a)
		{
			if (a == (AlignLeft | AlignTop))
				return TextAnchor.UpperLeft;
			else if (a == (AlignLeft | AlignVCenter))
				return TextAnchor.MiddleLeft;
			else if (a == (AlignLeft | AlignBottom))
				return TextAnchor.LowerLeft;
			else if (a == (AlignCenter | AlignTop))
				return TextAnchor.UpperCenter;
			else if (a == (AlignCenter | AlignVCenter))
				return TextAnchor.MiddleCenter;
			else if (a == (AlignCenter | AlignBottom))
				return TextAnchor.LowerCenter;
			else if (a == (AlignRight | AlignTop))
				return TextAnchor.UpperRight;
			else if (a == (AlignRight | AlignVCenter))
				return TextAnchor.MiddleRight;
			else if (a == (AlignRight | AlignBottom))
				return TextAnchor.LowerRight;
			else
				return TextAnchor.MiddleLeft;
		}

		public override string DebugLine
		{
			get
			{
				return base.DebugLine + " '" + text_ + "'";
			}
		}
	}
}
