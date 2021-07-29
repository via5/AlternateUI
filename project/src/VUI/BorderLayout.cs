﻿using System;
using System.Collections.Generic;

namespace VUI
{
	class BorderLayout : Layout
	{
		public override string TypeName { get { return "bl"; } }


		public class Data : LayoutData
		{
			public int side;

			public Data(int s)
			{
				side = s;
			}

			public static implicit operator int(Data d)
			{
				return d.side;
			}
		}

		public static Data Left = new Data(0);
		public static Data Top = new Data(1);
		public static Data Right = new Data(2);
		public static Data Bottom = new Data(3);
		public static Data Center = new Data(4);
		public static Data DefaultSide = Center;

		public const int TopLeft = 0;
		public const int TopRight = 1;
		public const int BottomLeft = 2;
		public const int BottomRight = 3;


		private readonly List<Widget>[] sides_ = new List<Widget>[5];
		private readonly int[] corners_ = new int[4];

		public BorderLayout(float spacing = 0)
		{
			Spacing = spacing;

			for (int i = 0; i < 5; ++i)
				sides_[i] = new List<Widget>();

			corners_[TopLeft] = Top;
			corners_[TopRight] = Top;
			corners_[BottomLeft] = Bottom;
			corners_[BottomRight] = Bottom;
		}

		public void SetCorner(int corner, int side)
		{
			if (corner < 0 || corner >= 4)
				return;

			if (side < 0 || side >= 4)
				return;

			corners_[corner] = side;
		}

		protected override void AddImpl(Widget w, LayoutData data = null)
		{
			var d = data as Data;
			if (d == null)
			{
				Glue.LogErrorST("BorderLayout: missing layout data");
				d = DefaultSide;
			}

			if (d.side < 0 || d.side > 5)
			{
				Glue.LogError(
					"bad border layout side " + d.side.ToString());

				return;
			}

			var s = sides_[d.side];
			s.Add(w);
		}

		protected override void RemoveImpl(Widget w)
		{
			foreach (var side in sides_)
			{
				if (side.Remove(w))
					return;
			}

			Glue.LogError(
				"border layout: can't remove '" + w.Name + "', not found");
		}

		protected override void LayoutImpl()
		{
			Rectangle av = new Rectangle(Parent.AbsoluteClientBounds);
			Rectangle center = new Rectangle(av);

			center.Top += DoTop(av);
			center.Bottom -= DoBottom(av);
			center.Left += DoLeft(av);
			center.Right -= DoRight(av);

			DoCenter(center);
		}

		protected override Size DoGetPreferredSize(float maxWidth, float maxHeight)
		{
			var left = SideWidth(Left, maxHeight);
			var right = SideWidth(Right, maxHeight);
			var top = SideHeight(Top, maxWidth);
			var bottom = SideHeight(Bottom, maxWidth);

			var center = new Size();
			foreach (var w in sides_[Center])
			{
				if (!w.Visible)
					continue;

				center = Size.Max(center, w.GetRealPreferredSize(maxWidth, maxHeight));
			}

			int hn =
				(left > 0 ? 1 : 0) +
				(center.Width > 0 ? 1 : 0) +
				(right > 0 ? 1 : 0);

			int vn =
				(top > 0 ? 1 : 0) +
				(center.Height > 0 ? 1 : 0) +
				(bottom > 0 ? 1 : 0);

			float width = left + center.Width + right;
			float height = top + center.Height + bottom;

			return new Size(
				width + (Math.Max(hn - 1, 0) * Spacing),
				height + (Math.Max(vn - 1, 0) * Spacing));
		}

		private float DoTop(Rectangle av)
		{
			float tallest = 0;

			foreach (var w in sides_[Top])
			{
				if (!w.Visible)
					continue;

				float wh = w.GetRealPreferredSize(av.Width, DontCare).Height;
				tallest = Math.Max(tallest, wh);

				Rectangle r = new Rectangle();

				r.Left = av.Left;
				r.Top = av.Top;
				r.Right = r.Left + av.Width;
				r.Bottom = r.Top + wh;

				var lw = SideWidth(Left, av.Height);
				var rw = SideWidth(Right, av.Height);

				if (corners_[TopLeft] != Top)
				{
					r.Left += lw;

					if (lw > 0)
						r.Left += Spacing;
				}

				if (corners_[TopRight] != Top)
				{
					r.Right -= rw;

					if (rw > 0)
						r.Right -= Spacing;
				}

				w.SetBounds(r);
			}

			if (tallest > 0)
				tallest += Spacing;

			return tallest;
		}

		private float DoBottom(Rectangle av)
		{
			float tallest = 0;

			foreach (var w in sides_[Bottom])
			{
				if (!w.Visible)
					continue;

				float wh = w.GetRealPreferredSize(av.Width, DontCare).Height;
				tallest = Math.Max(tallest, wh);

				Rectangle r = new Rectangle();

				r.Left = av.Left;
				r.Top = av.Bottom - wh;
				r.Right = r.Left + av.Width;
				r.Bottom = r.Top + wh;

				var lw = SideWidth(Left, av.Height);
				var rw = SideWidth(Right, av.Height);

				if (corners_[BottomLeft] != Bottom)
				{
					r.Left += lw;

					if (lw > 0)
						r.Left += Spacing;
				}

				if (corners_[BottomRight] != Bottom)
				{
					r.Right -= rw;

					if (rw > 0)
						r.Right -= Spacing;
				}

				w.SetBounds(r);
			}

			if (tallest > 0)
				tallest += Spacing;

			return tallest;
		}

		private float DoLeft(Rectangle av)
		{
			float widest = 0;

			foreach (var w in sides_[Left])
			{
				if (!w.Visible)
					continue;

				float ww = w.GetRealPreferredSize(DontCare, av.Height).Width;
				widest = Math.Max(widest, ww);

				Rectangle r = new Rectangle();

				r.Left = av.Left;
				r.Top = av.Top;

				r.Right = r.Left + ww;
				r.Bottom = r.Top + av.Height;

				var th = SideHeight(Top, av.Width);
				var bh = SideHeight(Bottom, av.Width);

				if (corners_[TopLeft] != Left)
				{
					r.Top += th;

					if (th > 0)
						r.Top += Spacing;
				}

				if (corners_[BottomLeft] != Left)
				{
					r.Bottom -= bh;

					if (bh > 0)
						r.Bottom -= Spacing;
				}

				w.SetBounds(r);
			}

			if (widest > 0)
				widest += Spacing;

			return widest;
		}

		private float DoRight(Rectangle av)
		{
			float widest = 0;

			foreach (var w in sides_[Right])
			{
				if (!w.Visible)
					continue;

				float ww = w.GetRealPreferredSize(DontCare, av.Height).Width;
				widest = Math.Max(widest, ww);

				Rectangle r = new Rectangle();

				r.Left = av.Right - ww;
				r.Top = av.Top;

				r.Right = r.Left + ww;
				r.Bottom = r.Top + av.Height;

				var th = SideHeight(Top, av.Width);
				var bh = SideHeight(Bottom, av.Width);

				if (corners_[TopRight] != Right)
				{
					r.Top += th;

					if (th > 0)
						r.Top += Spacing;
				}

				if (corners_[BottomRight] != Right)
				{
					r.Bottom -= bh;

					if (bh > 0)
						r.Bottom -= Spacing;
				}

				w.SetBounds(r);
			}

			if (widest > 0)
				widest += Spacing;

			return widest;
		}

		private void DoCenter(Rectangle av)
		{
			foreach (var w in sides_[Center])
			{
				if (!w.Visible)
					continue;

				w.SetBounds(av);
			}
		}

		private float SideWidth(int side, float maxHeight)
		{
			float widest = 0;

			foreach (var w in sides_[side])
			{
				if (!w.Visible)
					continue;

				float ww = w.GetRealPreferredSize(DontCare, maxHeight).Width;
				widest = Math.Max(widest, ww);
			}

			return widest;
		}

		private float SideHeight(int side, float maxWidth)
		{
			float tallest = 0;

			foreach (var w in sides_[side])
			{
				if (!w.Visible)
					continue;

				float wh = w.GetRealPreferredSize(maxWidth, DontCare).Height;
				tallest = Math.Max(tallest, wh);
			}

			return tallest;
		}
	}
}
