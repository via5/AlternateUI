using System;
using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	class Image : Widget
	{
		public override string TypeName { get { return "Image"; } }

		private RectTransform rt_ = null;
		private RawImage raw_ = null;
		private Texture tex_ = null;
		private static Material emptyMat_ = null;

		public Texture Texture
		{
			set
			{
				tex_ = value;
				UpdateTexture();
			}
		}

		protected override GameObject CreateGameObject()
		{
			return new GameObject();
		}

		protected override void DoCreate()
		{
			raw_ = WidgetObject.AddComponent<RawImage>();
			rt_ = WidgetObject.GetComponent<RectTransform>();

			if (emptyMat_ == null)
			{
				emptyMat_ = new Material(raw_.material);
				emptyMat_.mainTexture = Texture2D.blackTexture;
			}

			raw_.material = emptyMat_;
			UpdateTexture();
		}

		public override void UpdateBounds()
		{
			base.UpdateBounds();
		}

		private void UpdateTexture()
		{
			if (raw_ != null)
			{
				raw_.texture = tex_;

				Size scaled;

				if (tex_ == null)
				{
					scaled = ClientBounds.Size;
				}
				else
				{
					scaled = Aspect(
						tex_.width, tex_.height,
						ClientBounds.Width, ClientBounds.Height);
				}

				rt_.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, scaled.Width);
				rt_.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, scaled.Height);
			}
		}

		private Size Aspect(float width, float heigth, float maxWidth, float maxHeight)
		{
			float newWidth = width;
			float newHeight = heigth;

			if (width > heigth)
			{
				newWidth = maxWidth;
				newHeight = (newWidth * heigth) / width;
			}
			else
			{
				newHeight = maxHeight;
				newWidth = (newHeight * width) / heigth;
			}

			return new Size(newWidth, newHeight);
		}

		protected override void DoSetRender(bool b)
		{
			if (raw_ != null)
				raw_.gameObject.SetActive(b);
		}

		protected override Size DoGetPreferredSize(
			float maxWidth, float maxHeight)
		{
			Size s;

			if (tex_ == null)
			{
				s = new Size(
					Math.Max(maxWidth, maxHeight),
					Math.Max(maxWidth, maxHeight));
			}
			else
			{
				s = new Size(tex_.width, tex_.height);

				if (maxWidth != DontCare)
					s.Width = Math.Min(maxWidth, s.Width);

				if (maxHeight != DontCare)
					s.Height = Math.Min(maxHeight, s.Height);
			}

			s.Width = Math.Min(s.Width, s.Height);
			s.Height = Math.Min(s.Width, s.Height);

			return s;
		}
	}
}
