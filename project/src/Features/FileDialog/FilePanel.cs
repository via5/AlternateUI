namespace AUI.FileDialog
{
	class FilePanel
	{
		private string file_;
		private VUI.Panel panel_;
		private VUI.Image thumbnail_;
		private VUI.Label name_;
		private VUI.Timer thumbnailTimer_ = null;

		public FilePanel(int fontSize)
		{
			panel_ = new VUI.Panel(new VUI.BorderLayout());
			thumbnail_ = new VUI.Image();
			name_ = new VUI.Label();

			name_.FontSize = fontSize;
			name_.WrapMode = VUI.Label.ClipEllipsis;
			name_.Alignment = VUI.Label.AlignCenter | VUI.Label.AlignTop;
			//name_.MinimumSize = new VUI.Size(VUI.Widget.DontCare, 50);

			thumbnail_.Borders = new VUI.Insets(1);

			panel_.Add(thumbnail_, VUI.BorderLayout.Top);
			panel_.Add(name_, VUI.BorderLayout.Center);
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

			var t = Icons.GetFileIconFromCache(file_);

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

					Icons.GetFileIcon(file_, tt =>
					{
						if (file_ == forFile)
							thumbnail_.Texture = tt;
					});
				});
			}
			else
			{
				thumbnail_.Texture = t;
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
	}
}
