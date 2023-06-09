using System;
using UnityEngine;

namespace AUI
{
	class ThumbnailPanel : VUI.Panel
	{
		private readonly VUI.Image thumbnail_;
		private readonly VUI.Image packageIndicator_;
		private VUI.Timer thumbnailTimer_ = null;
		private int token_ = 0;

		public ThumbnailPanel(int leftPadding, int bottomPadding)
		{
			thumbnail_ = new VUI.Image();
			packageIndicator_ = new VUI.Image();

			packageIndicator_.Alignment = VUI.Align.BottomLeft;
			packageIndicator_.Margins = new VUI.Insets(leftPadding, 0, 0, bottomPadding);

			Layout = new VUI.BorderLayout();
			Add(thumbnail_, VUI.BorderLayout.Center);
			Add(packageIndicator_, VUI.BorderLayout.Center);
		}

		public void Set(DAZDynamicItem di)
		{
			if (di == null)
			{
				Clear();
				return;
			}

			++token_;

			SetDeferred((forToken) =>
			{
				di.GetThumbnail((Texture2D t) =>
				{
					if (token_ == forToken)
						SetTexture(t);
				});
			});

			SetPackageIndicator(di.IsInPackage);
		}

		public void Set(VUI.Icon i, bool inPackage)
		{
			if (i == null)
			{
				Clear();
				return;
			}

			++token_;

			if (i.CachedTexture != null)
			{
				SetTexture(i.CachedTexture);
			}
			else
			{
				SetDeferred((forToken) =>
				{
					i.GetTexture(t =>
					{
						if (token_ == forToken)
							SetTexture(t);
					});
				});
			}

			SetPackageIndicator(inPackage);
		}

		private void SetDeferred(Action<int> f)
		{
			int forToken = token_;

			if (thumbnailTimer_ != null)
			{
				thumbnailTimer_.Destroy();
				thumbnailTimer_ = null;
			}

			SetTexture(null);
			thumbnailTimer_ = VUI.TimerManager.Instance.CreateTimer(0.2f, () =>
			{
				if (token_ == forToken)
					f(forToken);
			});
		}

		private void SetPackageIndicator(bool inPackage)
		{
			if (inPackage)
			{
				packageIndicator_.Render = true;

				Icons.GetIcon(Icons.Package).GetTexture((t) =>
				{
					packageIndicator_.Texture = t;
				});
			}
			else
			{
				packageIndicator_.Render = false;
			}
		}

		public void Clear()
		{
			if (thumbnailTimer_ != null)
			{
				thumbnailTimer_.Destroy();
				thumbnailTimer_ = null;
			}

			thumbnail_.Texture = null;
		}

		private void SetTexture(Texture t)
		{
			if (t != null)
			{
				// some thumbnails are set to repeat, which adds spurious lines
				// on top when resizing
				t.wrapMode = TextureWrapMode.Clamp;
			}

			thumbnail_.Texture = t;
		}
	}
}
