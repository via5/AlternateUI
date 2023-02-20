namespace AUI.FileDialog
{
	class FilePanel
	{
		private readonly FileDialog fd_;
		private File file_ = null;

		private readonly VUI.Panel panel_;
		private readonly VUI.Image thumbnail_;
		private readonly VUI.Label name_;
		private VUI.Timer thumbnailTimer_ = null;

		public FilePanel(FileDialog fd, int fontSize)
		{
			fd_ = fd;

			panel_ = new VUI.Panel(new VUI.BorderLayout());
			thumbnail_ = new VUI.Image();
			name_ = new VUI.Label();

			name_.FontSize = fontSize;
			name_.WrapMode = VUI.Label.ClipEllipsis;
			name_.Alignment = VUI.Label.AlignCenter | VUI.Label.AlignTop;
			name_.MinimumSize = new VUI.Size(VUI.Widget.DontCare, 60);

			thumbnail_.Borders = new VUI.Insets(1);

			var thumbnailPanel = new VUI.Panel(new VUI.BorderLayout());
			thumbnailPanel.Add(thumbnail_, VUI.BorderLayout.Center);

			panel_.Add(thumbnailPanel, VUI.BorderLayout.Center);
			panel_.Add(name_, VUI.BorderLayout.Bottom);

			panel_.Padding = new VUI.Insets(8);
			panel_.Events.PointerClick += OnClick;
		}

		public File File
		{
			get { return file_; }
		}

		private void OnClick(VUI.PointerEvent e)
		{
			fd_.Select(this);
			e.Bubble = false;
		}

		public VUI.Panel Panel
		{
			get { return panel_; }
		}

		public void SetSelectedInternal(bool b)
		{
			if (b)
				panel_.BackgroundColor = VUI.Style.Theme.SelectionBackgroundColor;
			else
				panel_.BackgroundColor = new UnityEngine.Color(0, 0, 0, 0);
		}

		public void Set(File f)
		{
			file_ = f;

			name_.Text = Path.Filename(file_.Path);
			panel_.Render = true;

			var t = Icons.GetFileIconFromCache(file_.Path);

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
					File forFile = file_;

					Icons.GetFileIcon(file_.Path, tt =>
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
