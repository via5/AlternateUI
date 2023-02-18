using MVR.FileManagementSecure;

namespace AUI.FileDialog
{
	class FilePanel
	{
		private string file_;
		private VUI.Panel panel_;
		private VUI.Label name_;
		private VUI.Image thumbnail_;
		private VUI.Timer thumbnailTimer_ = null;

		public FilePanel(int fontSize)
		{
			panel_ = new VUI.Panel(new VUI.VerticalFlow());
			name_ = new VUI.Label();
			thumbnail_ = new VUI.Image();

			name_.FontSize = fontSize;
			name_.WrapMode = VUI.Label.ClipEllipsis;
			thumbnail_.Borders = new VUI.Insets(1);

			panel_.Add(name_);
			panel_.Add(thumbnail_);
		}

		public VUI.Panel Panel
		{
			get { return panel_; }
		}

		public void Set(string f)
		{
			file_ = f;
			name_.Text = Path.Filename(f);
			panel_.Render = true;

			var imgPath = GetThumbnailPath();
			if (imgPath == null)
			{
				thumbnail_.Texture = SuperController.singleton.fileBrowserUI.GetFileIcon(file_)?.texture;
			}
			else
			{
				var t = ImageLoaderThreaded.singleton.GetCachedThumbnail(imgPath);

				if (t == null)
				{
					if (thumbnailTimer_ != null)
					{
						thumbnailTimer_.Destroy();
						thumbnailTimer_ = null;
					}

					thumbnail_.Texture = null;

					thumbnailTimer_ = VUI.TimerManager.Instance.CreateTimer(0.2f, () =>
					{
						string forFile = file_;

						ImageLoaderThreaded.QueuedImage queuedImage = new ImageLoaderThreaded.QueuedImage();
						queuedImage.imgPath = imgPath;
						queuedImage.width = 200;
						queuedImage.height = 200;
						queuedImage.callback = (tt) =>
						{
							if (file_ == forFile)
								thumbnail_.Texture = tt.tex;
						};

						ImageLoaderThreaded.singleton.QueueThumbnail(queuedImage);
					});
				}
				else
				{
					thumbnail_.Texture = t;
				}
			}
		}

		public void Clear()
		{
			if (thumbnailTimer_ != null)
			{
				thumbnailTimer_.Destroy();
				thumbnailTimer_ = null;
			}

			name_.Text = "";
			thumbnail_.Texture = null;
			panel_.Render = false;
		}

		private string GetThumbnailPath()
		{
			var exts = new string[] { ".jpg", ".JPG" };

			foreach (var e in exts)
			{
				var relImgPath = Path.Parent(file_) + "\\" + Path.Stem(file_) + e;
				var imgPath = FileManagerSecure.GetFullPath(relImgPath);

				if (FileManagerSecure.FileExists(imgPath))
					return imgPath;
			}

			return null;
		}
	}
}
