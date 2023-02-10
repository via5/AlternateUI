using System;
using UnityEngine;
using UnityEngine.UI;

namespace VUI
{
	class Image : Widget
	{
		public override string TypeName { get { return "Image"; } }

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
				raw_.texture = tex_;
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
