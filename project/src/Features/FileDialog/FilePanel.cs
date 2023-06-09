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
		private readonly VUI.Label name_;
		private readonly ThumbnailPanel thumb_;
		private bool hovered_ = false;
		private bool selected_ = false;

		public FilePanel(FileDialog fd, int fontSize)
		{
			fd_ = fd;

			panel_ = new VUI.Panel(new VUI.BorderLayout());
			thumb_ = new ThumbnailPanel(10, 5);
			name_ = new VUI.Label();

			name_.FontSize = fontSize;
			name_.WrapMode = VUI.Label.ClipEllipsis;
			name_.Alignment = VUI.Align.TopCenter;
			name_.MinimumSize = new VUI.Size(VUI.Widget.DontCare, 60);


			panel_.Borders = new VUI.Insets(1);
			panel_.BorderColor = new Color(0, 0, 0, 0);
			panel_.Clickthrough = false;

			panel_.Add(thumb_, VUI.BorderLayout.Center);
			panel_.Add(name_, VUI.BorderLayout.Bottom);

			panel_.Padding = new VUI.Insets(8);
			panel_.Events.PointerClick += OnClick;
			panel_.Events.PointerDoubleClick += OnDoubleClick;
			panel_.Events.PointerEnter += OnPointerEnter;
			panel_.Events.PointerExit += OnPointerExit;

			// useless for now
			//panel_.ContextMenuMode = VUI.Widget.ContextMenuCallback;
			//panel_.CreateContextMenu += () => { return fd_.GetContextMenu(this); };
		}

		public Logger Log
		{
			get { return fd_.Log; }
		}

		public FS.IFilesystemObject Object
		{
			get { return o_; }
		}

		private void OnClick(VUI.PointerEvent e)
		{
			if (e.Button == VUI.PointerEvent.LeftButton || e.Button == VUI.PointerEvent.RightButton)
			{
				fd_.SelectFile(Object);
				e.Bubble = false;
			}
		}

		private void OnDoubleClick(VUI.PointerEvent e)
		{
			if (e.Button == VUI.PointerEvent.LeftButton)
			{
				fd_.Activate(this);
				e.Bubble = false;
			}
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

		public void Set(FS.Context cx, FS.IFilesystemObject o)
		{
			o_ = o;

			name_.Text = o_.GetDisplayName(cx);
			panel_.Tooltip.Text = o_.Tooltip;
			panel_.Tooltip.FontSize = name_.FontSize;
			panel_.Render = true;
			SetIcon();
		}

		private void SetIcon()
		{
			thumb_.Set(o_.Icon, (o_.ParentPackage != null));
		}

		public void Clear()
		{
			name_.Text = "";
			thumb_.Clear();
			panel_.Render = false;
			selected_ = false;
			hovered_ = false;
		}
	}


	class FilesPanel : VUI.Panel
	{
		private readonly FileDialog fd_;
		private readonly Logger log_;
		private readonly int cols_, rows_;
		private readonly VUI.FixedScrolledPanel scroll_;
		private readonly FilePanel[] panels_;
		private List<FS.IFilesystemObject> files_ = null;
		private bool ignoreScroll_ = false;

		public FilesPanel(FileDialog fd, int cols, int rows)
		{
			fd_ = fd;
			log_ = new Logger("fd.filesPanel");
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

		public new Logger Log
		{
			get { return log_; }
		}

		public void SetFiles(List<FS.IFilesystemObject> files)
		{
			Log.Info($"SetFiles count={files.Count}");
			files_ = files;
		}

		public void SetSelected(FS.IFilesystemObject o, bool b, bool scroll)
		{
			Log.Info($"SetSelected {b} {o} scroll={scroll}");

			var p = FindPanel(o);

			if (p != null)
			{
				Log.Info($"  - found in visible panels, sel={b}");

				p.SetSelectedInternal(b);

				//if (b)
				//	SetPanels(scroll_.Top * cols_);

				return;
			}

			if (b && scroll)
			{
				for (int i = 0; i < files_.Count; ++i)
				{
					if (files_[i] == o)
					{
						int newTop = MakeTop(i);

						Log.Info($"  - scrolling, i={i} newtop={newTop} currenttop={scroll_.Top}");
						SetScrollPanel("selection", newTop);

						break;
					}
				}
			}
			else
			{
				Log.Info($"  - can't select {b} {o}, scroll is false and panel is not visible");
			}
		}

		private int MakeTop(int fileIndex)
		{
			int top = fileIndex / cols_;
			int totalRows = (int)Math.Ceiling((float)files_.Count / cols_);

			if (top + rows_ > totalRows)
				top = Math.Max(totalRows - rows_, 0);

			return top;
		}

		public void ScrollToTop()
		{
			if (files_ == null)
			{
				Log.ErrorST("ScrollToTop() called with no files");
				return;
			}

			Log.Info("scroll to top");

			SetPanels(0);
			SetScrollPanel("scroll to top", -1);
		}

		public void Clear()
		{
			Log.Info("clearing panels");

			for (int i = 0; i < panels_.Length; ++i)
				panels_[i]?.Clear();
		}

		private void SetPanels(int from)
		{
			Log.Info($"SetPanels from {from}");

			int count = files_?.Count ?? 0;
			var cx = fd_.CreateFileContext(false);

			int panelIndex = 0;
			for (int i = from; i < count; ++i)
			{
				var f = files_[i];
				var fp = panels_[panelIndex];

				fp.Set(cx, f);
				fp.SetSelectedInternal(fd_.SelectedFile?.IsSameObject(f) ?? false);

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

		private void SetScrollPanel(string why, int setTop)
		{
			AlternateUI.Instance.StartCoroutine(CoSetScrollPanel(why, setTop));
		}

		private IEnumerator CoSetScrollPanel(string why, int setTop)
		{
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();

			Log.Info($"CoSetScrollPanel why={why}");

			int totalRows = (int)Math.Ceiling((float)files_.Count / cols_);
			int offscreenRows = totalRows - rows_;
			float scrollbarSize = scroll_.ContentPanel.ClientBounds.Height / rows_ / 3;
			float pos = 0;

			if (setTop >= 0)
				pos = setTop * scrollbarSize;

			try
			{
				Log.Info($"CoSetScrollPanel scroll_.Set offscreenRows={offscreenRows} pos={pos}");

				ignoreScroll_ = true;
				scroll_.Set(offscreenRows, scrollbarSize, pos);
			}
			finally
			{
				ignoreScroll_ = false;
			}

			Log.Info($"CoSetScrollPanel, calling OnScroll() with setTop={setTop} top={scroll_.Top}");
			OnScroll(scroll_.Top);
		}

		private void OnScroll(int top)
		{
			if (ignoreScroll_) return;

			Log.Info($"OnScroll top={top}");
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
