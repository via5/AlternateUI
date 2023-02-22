using MVR.FileManagementSecure;
using UnityEngine;

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

			panel_.Borders = new VUI.Insets(1);
			panel_.BorderColor = new Color(0, 0, 0, 0);

			var thumbnailPanel = new VUI.Panel(new VUI.BorderLayout());
			thumbnailPanel.Add(thumbnail_, VUI.BorderLayout.Center);

			panel_.Add(thumbnailPanel, VUI.BorderLayout.Center);
			panel_.Add(name_, VUI.BorderLayout.Bottom);

			panel_.Padding = new VUI.Insets(8);
			panel_.Events.PointerClick += OnClick;
			panel_.Events.PointerDoubleClick += OnDoubleClick;
			panel_.Events.PointerEnter += OnPointerEnter;
			panel_.Events.PointerExit += OnPointerExit;
		}

		public File File
		{
			get { return file_; }
		}

		private void OnClick(VUI.PointerEvent e)
		{
			if (e.Button == VUI.PointerEvent.LeftButton)
				fd_.Select(File);

			e.Bubble = false;
		}

		private void OnDoubleClick(VUI.PointerEvent e)
		{
			if (e.Button == VUI.PointerEvent.LeftButton)
				fd_.Activate(this);

			e.Bubble = false;
		}

		private void OnPointerEnter(VUI.PointerEvent e)
		{
			if (fd_.Selected != File)
			{
				panel_.BackgroundColor = VUI.Style.Theme.HighlightBackgroundColor;
				panel_.BorderColor = VUI.Style.Theme.BorderColor;
			}
		}

		private void OnPointerExit(VUI.PointerEvent e)
		{
			if (fd_.Selected != File)
			{
				panel_.BackgroundColor = new Color(0, 0, 0, 0);
				panel_.BorderColor = new Color(0, 0, 0, 0);
			}
		}

		public VUI.Panel Panel
		{
			get { return panel_; }
		}

		public void SetSelectedInternal(bool b)
		{
			if (b)
			{
				panel_.BackgroundColor = VUI.Style.Theme.SelectionBackgroundColor;
				panel_.BorderColor = VUI.Style.Theme.BorderColor;
			}
			else
			{
				panel_.BackgroundColor = new UnityEngine.Color(0, 0, 0, 0);
				panel_.BorderColor = new Color(0, 0, 0, 0);
			}
		}

		public void Set(File f)
		{
			file_ = f;

			name_.Text = file_.Filename;
			panel_.Tooltip.Text = MakeTooltip();
			panel_.Render = true;
			SetIcon();
		}

		private string MakeTooltip()
		{
			string tt = $"Path: {file_.Path}";

			if (FileManagerSecure.IsFileInPackage(file_.Path) ||
				FileManagerSecure.IsDirectoryInPackage(file_.Path))
			{
				var col = file_.Path.IndexOf(":");
				if (col != -1)
				{
					var s = file_.Path.Substring(0, col);

					if (FileManagerSecure.PackageExists(s))
						tt += $"\nPackage: {s}";
					else
						tt += $"\nPackage: {s} (not found)";
				}
			}

			tt += $"\nCreated: {FileManagerSecure.FileCreationTime(file_.Path)}";
			tt += $"\nLast modified: {FileManagerSecure.FileLastWriteTime(file_.Path)}";

			return tt;
		}

		private void SetIcon()
		{
			var t = Icons.GetFileIconFromCache(file_.Path);

			if (t == null)
			{
				if (thumbnailTimer_ != null)
				{
					thumbnailTimer_.Destroy();
					thumbnailTimer_ = null;
				}

				SetTexture(null);

				thumbnailTimer_ = VUI.TimerManager.Instance.CreateTimer(0.2f, () =>
				{
					File forFile = file_;

					Icons.GetFileIcon(file_.Path, tt =>
					{
						if (file_ == forFile)
						{
							SetTexture(tt);
						}
					});
				});
			}
			else
			{
				SetTexture(t);
			}
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
