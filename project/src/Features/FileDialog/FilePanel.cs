using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AUI.FileDialog
{
	class FilePanel
	{
		private readonly FileDialog fd_;
		private FS.IFilesystemObject o_ = null;

		private readonly VUI.Panel panel_;
		private readonly VUI.Image thumbnail_;
		private readonly VUI.Label name_;
		private VUI.Timer thumbnailTimer_ = null;
		private bool hovered_ = false;
		private bool selected_ = false;

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
			panel_.Clickthrough = false;

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

		public FS.IFilesystemObject Object
		{
			get { return o_; }
		}

		private void OnClick(VUI.PointerEvent e)
		{
			if (e.Button == VUI.PointerEvent.LeftButton)
				fd_.SelectFile(Object);

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
			hovered_ = true;
			UpdateBackground();
		}

		private void OnPointerExit(VUI.PointerEvent e)
		{
			hovered_ = false;
			UpdateBackground();
		}

		public void SetSelectedInternal(bool b)
		{
			selected_ = b;
			UpdateBackground();
		}

		private void UpdateBackground()
		{
			if (selected_)
			{
				panel_.BackgroundColor = VUI.Style.Theme.SelectionBackgroundColor;
				panel_.BorderColor = VUI.Style.Theme.BorderColor;
			}
			else
			{
				if (hovered_)
				{
					panel_.BackgroundColor = VUI.Style.Theme.HighlightBackgroundColor;
					panel_.BorderColor = VUI.Style.Theme.BorderColor;
				}
				else
				{
					panel_.BackgroundColor = new Color(0, 0, 0, 0);
					panel_.BorderColor = new Color(0, 0, 0, 0);
				}
			}
		}

		public VUI.Panel Panel
		{
			get { return panel_; }
		}

		public void Set(FS.IFilesystemObject o)
		{
			o_ = o;

			name_.Text = o_.DisplayName;
			panel_.Tooltip.Text = o_.Tooltip;
			panel_.Tooltip.FontSize = name_.FontSize;
			panel_.Render = true;
			SetIcon();
		}

		private void SetIcon()
		{
			var i = o_.Icon;

			if (i.CachedTexture != null)
			{
				SetTexture(i.CachedTexture);
			}
			else
			{
				if (thumbnailTimer_ != null)
				{
					thumbnailTimer_.Destroy();
					thumbnailTimer_ = null;
				}

				SetTexture(null);
				var forObject = o_;

				thumbnailTimer_ = VUI.TimerManager.Instance.CreateTimer(0.2f, () =>
				{
					i.GetTexture(t =>
					{
						if (o_ == forObject)
							SetTexture(t);
					});
				});
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
			selected_ = false;
			hovered_ = false;
		}
	}


	class FilesPanel : VUI.Panel
	{
		private readonly FileDialog fd_;
		private readonly int cols_, rows_;
		private readonly VUI.FixedScrolledPanel scroll_;
		private readonly FilePanel[] panels_;
		private List<FS.IFilesystemObject> files_ = null;
		private float setScroll_ = -1;
		private int setTop_ = -1;

		public FilesPanel(FileDialog fd, int cols, int rows)
		{
			fd_ = fd;
			cols_ = cols;
			rows_ = rows;

			scroll_ = new VUI.FixedScrolledPanel();

			var p = scroll_.ContentPanel;

			var gl = new VUI.GridLayout(cols, 10);
			gl.UniformWidth = true;

			p.Layout = gl;
			p.Padding = new VUI.Insets(0, 0, 5, 0);

			panels_ = new FilePanel[cols * rows];

			for (int j = 0; j < cols * rows; ++j)
			{
				panels_[j] = new FilePanel(fd, FileDialog.FontSize);
				p.Add(panels_[j].Panel);
			}

			scroll_.Scrolled += OnScroll;
			scroll_.Events.PointerClick += OnClicked;

			Layout = new VUI.BorderLayout();
			Add(scroll_, VUI.BorderLayout.Center);
		}

		public void SetFiles(List<FS.IFilesystemObject> files)
		{
			files_ = files;
			SetPanels(0);
			AlternateUI.Instance.StartCoroutine(CoSetScrollPanel());
		}

		public void SetSelected(FS.IFilesystemObject o, bool b, bool scroll)
		{
			var p = FindPanel(o);
			if (p != null)
			{
				p.SetSelectedInternal(b);
				return;
			}

			if (b && scroll)
			{
				for (int i = 0; i < files_.Count; ++i)
				{
					if (files_[i] == o)
					{
						setTop_ = i / cols_;
						SetSelected(o, true, false);
						AlternateUI.Instance.StartCoroutine(CoSetScrollPanel());
						break;
					}
				}
			}
		}

		public void ScrollToTop()
		{
			SetPanels(0);
		}

		public float Scroll
		{
			get
			{
				return scroll_.VerticalScrollbar.Value;
			}

			set
			{
				scroll_.VerticalScrollbar.Value = value;
				setScroll_ = value;
			}
		}

		public void Clear()
		{
			for (int i = 0; i < panels_.Length; ++i)
				panels_[i]?.Clear();
		}

		private void SetPanels(int from)
		{
			int count = files_?.Count ?? 0;

			int panelIndex = 0;
			for (int i = from; i < count; ++i)
			{
				var f = files_[i];
				var fp = panels_[panelIndex];

				fp.Set(f);
				fp.SetSelectedInternal(fd_.SelectedFile == f);

				++panelIndex;
				if (panelIndex >= (cols_ * rows_))
					break;
			}

			while (panelIndex < (cols_ * rows_))
			{
				panels_[panelIndex].Clear();
				++panelIndex;
			}
		}

		private FilePanel FindPanel(FS.IFilesystemObject o)
		{
			for (int i = 0; i < panels_.Length; ++i)
			{
				if (panels_[i].Object == o)
					return panels_[i];
			}

			return null;
		}

		private IEnumerator CoSetScrollPanel()
		{
			yield return new WaitForEndOfFrame();

			int totalRows = (int)Math.Ceiling((float)files_.Count / cols_);
			int offscreenRows = totalRows - rows_;
			float scrollbarSize = scroll_.ContentPanel.ClientBounds.Height / rows_ / 3;

			float pos = 0;

			if (setScroll_ >= 0)
			{
				pos = (setScroll_ >= 0 ? setScroll_ : 0);
				setScroll_ = -1;
			}
			else if (setTop_ >= 0)
			{
				pos = setTop_ * scrollbarSize;
				setTop_ = -1;
			}

			scroll_.Set(offscreenRows, scrollbarSize, pos);
		}

		private void OnScroll(int top)
		{
			SetPanels(top * cols_);
		}

		private void OnClicked(VUI.PointerEvent e)
		{
			if (e.Button == VUI.PointerEvent.LeftButton)
				fd_.SelectFile(null);

			e.Bubble = false;
		}
	}
}
