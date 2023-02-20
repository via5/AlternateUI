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
			var rawObject = new GameObject();
			rawObject.transform.SetParent(WidgetObject.transform, false);

			raw_ = rawObject.AddComponent<RawImage>();
			rt_ = rawObject.GetComponent<RectTransform>();

			rt_.anchorMin = new Vector2(0, 0);
			rt_.anchorMax = new Vector2(1, 1);
			rt_.offsetMin = new Vector2(0, 0);
			rt_.offsetMax = new Vector2(0, 0);

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
			UpdateAspect();
		}

		private void UpdateTexture()
		{
			if (raw_ != null)
			{
				raw_.texture = tex_;
				UpdateAspect();
			}
		}

		private void UpdateAspect()
		{
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

		private Size Aspect(float width, float height, float maxWidth, float maxHeight)
		{
			double ratioX = (double)maxWidth / (double)width;
			double ratioY = (double)maxHeight / (double)height;
			double ratio = ratioX < ratioY ? ratioX : ratioY;

			int newHeight = Convert.ToInt32(Math.Round(height * ratio));
			int newWidth = Convert.ToInt32(Math.Round(width * ratio));

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
