﻿using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AUI.FileDialog
{
	//class Options
	//{
	//	public bool flattenDirectories = true;
	//	public bool flattenPackages = true;
	//	public int sort = FS.Filter.SortFilename;
	//	public int sortDir = FS.Filter.SortAscending;
	//
	//	public void Set(IFileDialogMode mode)
	//	{
	//		flattenDirectories = mode.GetDefaultFlattenDirectories();
	//	}
	//}


	public class ExtensionItem
	{
		private readonly string text_;
		private readonly string[] exts_;

		public ExtensionItem(string text, string[] exts)
		{
			text_ = text;
			exts_ = exts;
		}

		public string[] Extensions
		{
			get { return exts_; }
		}

		public override string ToString()
		{
			return text_;
		}
	}


	class ButtonsPanel : VUI.Panel
	{
		public delegate void Handler();
		public event Handler FilenameChanged;

		private readonly FileDialog fd_;
		private readonly VUI.Button action_;
		private readonly VUI.TextBox filename_;
		private readonly VUI.ComboBox<ExtensionItem> extensions_;

		private bool ignore_ = false;

		public ButtonsPanel(FileDialog fd)
		{
			fd_ = fd;

			Layout = new VUI.VerticalFlow(20);
			Padding = new VUI.Insets(20);
			Borders = new VUI.Insets(0, 1, 0, 0);

			var fn = new VUI.Panel(new VUI.BorderLayout(10));
			fn.Padding = new VUI.Insets(30, 0, 0, 0);
			fn.Add(new VUI.Label("File name:"), VUI.BorderLayout.Left);

			filename_ = fn.Add(new VUI.TextBox(), VUI.BorderLayout.Center);
			filename_.Changed += OnFilenameChanged;
			filename_.Submitted += (s) => fd_.ExecuteAction();

			extensions_ = fn.Add(new VUI.ComboBox<ExtensionItem>(), VUI.BorderLayout.Right);
			extensions_.MinimumSize = new VUI.Size(500, VUI.Widget.DontCare);
			extensions_.MaximumSize = new VUI.Size(500, VUI.Widget.DontCare);
			extensions_.PopupWidth = 500;
			extensions_.SelectionChanged += OnExtensionChanged;

			var buttons = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.FlowLayout.AlignRight | VUI.FlowLayout.AlignVCenter));
			action_ = buttons.Add(new VUI.Button("", () => fd_.ExecuteAction()));
			buttons.Add(new VUI.Button("Cancel", () => fd_.Cancel()));

			Add(fn);
			Add(buttons);
		}

		public string Filename
		{
			get
			{
				return filename_.Text;
			}

			set
			{
				try
				{
					ignore_ = true;
					filename_.Text = value;
				}
				finally
				{
					ignore_ = false;
				}
			}
		}

		public ExtensionItem SelectedExtension
		{
			get
			{
				return extensions_.Selected;
			}
		}

		public void Set(IFileDialogMode mode)
		{
			try
			{
				ignore_ = true;
				action_.Text = mode.ActionText;
				extensions_.SetItems(mode.Extensions);
				filename_.Text = "";
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void FocusFilename()
		{
			filename_.Focus();
		}

		public void SetActionButton(bool enabled)
		{
			action_.Enabled = enabled;
		}

		private void OnExtensionChanged(ExtensionItem e)
		{
			if (ignore_) return;
			fd_.RefreshFiles();
		}

		private void OnFilenameChanged(string s)
		{
			if (ignore_) return;
			FilenameChanged?.Invoke();
		}
	}


	class OptionsPanel : VUI.Panel
	{
		public delegate void Handler();
		public event Handler DirectoriesChanged, FilesChanged;

		private readonly FileDialog fd_;
		private IFileDialogMode mode_ = null;

		private readonly VUI.CheckBox flattenDirs_;
		private readonly VUI.CheckBox flattenPackages_;
		private readonly VUI.MenuButton sortPanel_;

		private readonly Dictionary<int, VUI.RadioMenuItem> sortItems_ =
			new Dictionary<int, VUI.RadioMenuItem>();

		private readonly Dictionary<int, VUI.RadioMenuItem> sortDirItems_ =
			new Dictionary<int, VUI.RadioMenuItem>();

		private bool ignore_ = false;


		public OptionsPanel(FileDialog fd)
		{
			fd_ = fd;

			var sortMenu = new VUI.Menu();

			AddSortItem(sortMenu, "Filename", FS.Filter.SortFilename);
			AddSortItem(sortMenu, "Type", FS.Filter.SortType);
			AddSortItem(sortMenu, "Date modified", FS.Filter.SortDateModified);
			AddSortItem(sortMenu, "Date created", FS.Filter.SortDateCreated);
			sortMenu.AddSeparator();
			AddSortDirItem(sortMenu, "Ascending", FS.Filter.SortAscending);
			AddSortDirItem(sortMenu, "Descending", FS.Filter.SortDescending);

			sortPanel_ = new VUI.MenuButton("Sort", sortMenu);

			Layout = new VUI.HorizontalFlow(10, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter);

			flattenDirs_ = Add(new VUI.CheckBox("Flatten folders", SetFlattenDirectories));
			flattenPackages_ = Add(new VUI.CheckBox("Flatten package content", SetFlattenPackages));
			Add(sortPanel_.Button);
		}

		private void AddSortItem(VUI.Menu menu, string text, int sort)
		{
			var item = menu.AddMenuItem(MakeSortItem(text, sort));
			sortItems_.Add(sort, item);
		}

		private void AddSortDirItem(VUI.Menu menu, string text, int sort)
		{
			var item = menu.AddMenuItem(MakeSortDirItem(text, sort));
			sortDirItems_.Add(sort, item);
		}

		public void Set(IFileDialogMode mode)
		{
			try
			{
				ignore_ = true;

				mode_ = mode;

				flattenDirs_.Visible = !mode_.IsWritable;
				flattenPackages_.Visible = !mode_.IsWritable;

				flattenDirs_.Checked = mode_.FlattenDirectories;
				flattenPackages_.Checked = mode_.FlattenPackages;

				VUI.RadioMenuItem item;

				if (sortItems_.TryGetValue(mode_.Sort, out item))
					item.RadioButton.Checked = true;

				if (sortDirItems_.TryGetValue(mode_.SortDirection, out item))
					item.RadioButton.Checked = true;
			}
			finally
			{
				ignore_ = false;
			}
		}

		private VUI.RadioMenuItem MakeSortItem(string text, int sort)
		{
			VUI.RadioButton.ChangedCallback cb = (bool b) =>
			{
				if (b)
					SetSort(sort);
			};

			return new VUI.RadioMenuItem(text, cb, false, "sort");
		}

		private VUI.RadioMenuItem MakeSortDirItem(string text, int sortDir)
		{
			VUI.RadioButton.ChangedCallback cb = (bool b) =>
			{
				if (b)
					SetSortDirection(sortDir);
			};

			return new VUI.RadioMenuItem(text, cb, false, "sortDir");
		}

		private void SetFlattenDirectories(bool b)
		{
			if (ignore_) return;

			mode_.FlattenDirectories = b;
			DirectoriesChanged?.Invoke();
		}

		private void SetFlattenPackages(bool b)
		{
			if (ignore_) return;

			mode_.FlattenPackages = b;
			DirectoriesChanged?.Invoke();
		}

		private void SetSort(int s)
		{
			if (ignore_) return;

			mode_.Sort = s;
			FilesChanged?.Invoke();
		}

		private void SetSortDirection(int s)
		{
			if (ignore_) return;

			mode_.SortDirection = s;
			FilesChanged?.Invoke();
		}
	}


	class AddressBar : VUI.Panel
	{
		private readonly FileDialog fd_;

		private readonly VUI.ToolButton back_, next_, up_, refresh_;
		private readonly VUI.MenuButton drop_;
		private readonly VUI.ToolButton pin_, openInExplorer_;
		private readonly VUI.TextBox path_;
		private readonly SearchBox search_;
		private readonly VUI.Menu dropMenu_;
		private bool ignore_ = false;

		public AddressBar(FileDialog fd)
		{
			fd_ = fd;

			Layout = new VUI.BorderLayout();

			var left = new VUI.Panel(new VUI.BorderLayout(10));

			dropMenu_ = new VUI.Menu();
			drop_ = new VUI.MenuButton("v", true, true, dropMenu_);
			drop_.AboutToOpen += UpdateHistoryMenu;
			drop_.CloseOnMenuActivated = true;

			var buttons = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.FlowLayout.AlignLeft | VUI.FlowLayout.AlignVCenter, true));
			back_ = buttons.Add(new VUI.ToolButton("\x2190", () => fd_.Back(), "Back"));
			next_ = buttons.Add(new VUI.ToolButton("\x2192", () => fd_.Next(), "Next"));
			buttons.Add(drop_.Button);
			up_ = buttons.Add(new VUI.ToolButton("\x2191", () => fd_.Up(), "Up"));
			refresh_ = buttons.Add(new VUI.ToolButton("Refresh", () => fd_.Refresh(), "Refresh"));
			pin_ = buttons.Add(new VUI.ToolButton("Pin", OnTogglePin));
			openInExplorer_ = buttons.Add(new VUI.ToolButton("Explorer", OpenInExplorer, "Open in Explorer"));

			back_.Icon = Icons.Get(Icons.Back);
			back_.SetBorderless();

			next_.Icon = Icons.Get(Icons.Next);
			next_.SetBorderless();

			drop_.Button.Icon = Icons.Get(Icons.Drop);
			drop_.Button.IconSize = new VUI.Size(16, 20);
			drop_.Button.SetBorderless();
			drop_.Button.Tooltip.Text = "Recent locations";

			up_.Icon = Icons.Get(Icons.Up);
			up_.SetBorderless();

			refresh_.Icon = Icons.Get(Icons.Reload);
			refresh_.SetBorderless();

			openInExplorer_.Icon = Icons.Get(Icons.OpenExternal);
			openInExplorer_.SetBorderless();

			pin_.Icon = Icons.Get(Icons.Unpinned);
			pin_.SetBorderless();
			pin_.Borders = new VUI.Insets(1);
			pin_.BorderColor = new Color(0, 0, 0, 0);

			left.Add(buttons, VUI.BorderLayout.Left);
			path_ = left.Add(new VUI.TextBox(), VUI.BorderLayout.Center);
			path_.Submitted += OnPathSubmitted;

			search_ = new SearchBox("Search");
			search_.MinimumSize = new VUI.Size(400, VUI.Widget.DontCare);
			search_.Changed += OnSearchChanged;

			var sp = new VUI.Splitter(left, search_, VUI.Splitter.MinimumSecond);

			Add(sp, VUI.BorderLayout.Center);
		}

		public string Search
		{
			get { return search_.Text; }
		}

		public string Path
		{
			get { return path_.Text; }
			set { path_.Text = value; }
		}

		public void SetDirectory(FS.IFilesystemContainer dir)
		{
			try
			{
				ignore_ = true;

				back_.Enabled = fd_.CanGoBack();
				next_.Enabled = fd_.CanGoNext();
				up_.Enabled = fd_.CanGoUp();
				drop_.Button.Enabled = (fd_.CanGoBack() || fd_.CanGoNext());
				openInExplorer_.Enabled = !dir.Virtual;

				UpdatePin();
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void ClearSearch()
		{
			try
			{
				ignore_ = true;
				search_.Text = "";
			}
			finally
			{
				ignore_ = false;
			}
		}

		public void DropHistory()
		{
			drop_.Show();
		}

		public void UpdateHistoryMenu()
		{
			dropMenu_.Clear();

			var entries = fd_.History.Entries;
			for (int ri=0; ri<entries.Length; ++ri)
			{
				int i = entries.Length - ri - 1;
				bool check = (i == fd_.History.CurrentIndex);

				var mi = dropMenu_.AddMenuItem(new VUI.RadioMenuItem(entries[i], (b) =>
				{
					if (b)
						OnHistoryEntry(i);
				}, check, "history"));

				mi.RadioButton.FontSize = FileDialog.FontSize;
			}
		}

		public void OpenInExplorer()
		{
			var dir = fd_.SelectedDirectory;

			if (dir != null && !dir.Virtual)
				SuperController.singleton.OpenFolderInExplorer(dir.MakeRealPath());
		}

		private void UpdatePin()
		{
			var dir = fd_.SelectedDirectory;

			bool enabled = (dir?.CanPin ?? false);
			bool pinned = (dir != null && FS.Filesystem.Instance.IsPinned(dir));

			pin_.Enabled = enabled;

			if (pinned)
			{
				pin_.Text = "Unpin";
				pin_.Tooltip.Text = "Unpin";
				pin_.Icon = Icons.Get(Icons.Pinned);
				pin_.BorderColor = VUI.Style.Theme.BorderColor;
			}
			else
			{
				pin_.Text = "Pin";
				pin_.Tooltip.Text = "Pin";
				pin_.Icon = Icons.Get(Icons.Unpinned);
				pin_.BorderColor = new Color(0, 0, 0, 0);
			}
		}

		private void OnHistoryEntry(int i)
		{
			fd_.GoHistory(i);
		}

		private void OnTogglePin()
		{
			if (ignore_) return;

			var s = fd_.SelectedDirectory;
			if (s != null && s.CanPin)
			{
				if (FS.Filesystem.Instance.IsPinned(s))
					FS.Filesystem.Instance.Unpin(s);
				else
					FS.Filesystem.Instance.Pin(s);

				UpdatePin();
			}
		}

		private void OnPathSubmitted(string s)
		{
			if (ignore_) return;
			fd_.SelectDirectory(s);
		}

		private void OnSearchChanged(string s)
		{
			if (ignore_) return;
			fd_.RefreshFiles();
		}
	}


	class FilesPanel : VUI.Panel
	{
		private readonly FileDialog fd_;
		private readonly int cols_, rows_;
		private readonly VUI.FixedScrolledPanel scroll_;
		private readonly FilePanel[] panels_;
		private List<FS.IFilesystemObject> files_ = null;

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

		public void SetSelected(FS.IFilesystemObject o, bool b)
		{
			FindPanel(o)?.SetSelectedInternal(b);
		}

		public void ScrollToTop()
		{
			SetPanels(0);
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

			scroll_.Set(offscreenRows, scrollbarSize);
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


	class FileDialog
	{
		public const int FontSize = 24;

		private readonly Logger log_;

		private VUI.Root root_ = null;
		private VUI.Window window_ = null;

		private OptionsPanel optionsPanel_ = null;
		private AddressBar addressBar_ = null;
		private FileTree tree_ = null;
		private FilesPanel filesPanel_ = null;
		private ButtonsPanel buttonsPanel_ = null;

		private IFileDialogMode mode_ = new NoMode();
		private FS.IFilesystemContainer dir_ = null;
		private List<FS.IFilesystemObject> files_ = null;
		private FS.IFilesystemObject selected_ = null;
		private Action<string> callback_ = null;
		private bool ignoreHistory_ = false;


		public FileDialog()
		{
			log_ = new Logger("filedialog");
		}


		public void Enable()
		{
		}

		public void Disable()
		{
			if (root_ != null)
				root_.Visible = false;
		}

		public void Update(float s)
		{
			root_?.Update();
		}


		public Logger Log
		{
			get { return log_; }
		}

		public int Columns
		{
			get { return 5; }
		}

		public int Rows
		{
			get { return 4; }
		}


		public FS.IFilesystemContainer SelectedDirectory
		{
			get { return dir_; }
		}

		public FS.IFilesystemObject SelectedFile
		{
			get { return selected_; }
		}

		public string Filename
		{
			get { return buttonsPanel_.Filename; }
		}

		public History History
		{
			get { return mode_.History; }
		}


		public void Show(IFileDialogMode mode, Action<string> callback)
		{
			if (root_ == null)
				CreateUI();

			mode_ = mode;
			callback_ = callback;

			root_.Visible = true;
			window_.Title = mode_.Title;

			dir_ = null;
			filesPanel_.Clear();
			optionsPanel_.Set(mode_);
			buttonsPanel_.Set(mode_);

			var scrollTo = VUI.TreeView.ScrollToCenter;

			if (!SelectDirectory(mode_.CurrentDirectory, false, scrollTo))
			{
				Log.Error($"bad directory {mode_.CurrentDirectory}");

				if (SelectDirectory(mode_.DefaultDirectory, false, scrollTo))
				{
					mode_.CurrentDirectory = mode_.DefaultDirectory;
				}
				else
				{
					Log.Error($"bad directory {mode_.DefaultDirectory}");
					mode_.CurrentDirectory = FS.Filesystem.Instance.GetRootDirectory().VirtualPath;
					SelectDirectory(mode_.CurrentDirectory, false, scrollTo);
				}
			}

			if (dir_ == null)
			{
				// SelectDirectory() above won't fire the selected event if the
				// node was already selected, so call it manually to make sure
				SetCurrentDirectory(tree_.Selected as FS.IFilesystemContainer);
			}

			SelectFile(null);

			tree_.Enable();
			tree_.SetFlags(GetTreeFlags());

			buttonsPanel_.FocusFilename();
		}

		public void Hide()
		{
			tree_.Disable();
			root_.Visible = false;
		}

		public void SelectFile(FS.IFilesystemObject o)
		{
			if (o == null)
				o = new FS.NullDirectory();

			if (selected_ == o)
			{
				buttonsPanel_.Filename = selected_?.DisplayName ?? "";
			}
			else
			{
				if (selected_ != null)
					filesPanel_.SetSelected(selected_, false);

				selected_ = o;
				buttonsPanel_.Filename = selected_?.DisplayName ?? "";

				if (selected_ != null)
					filesPanel_.SetSelected(selected_, true);

				UpdateActionButton();
			}
		}

		public bool SelectDirectory(
			string vpath, bool expand = true,
			int scrollTo = VUI.TreeView.ScrollToNearest)
		{
			if (!tree_.Select(vpath, expand, scrollTo))
			{
				SetPath();
				return false;
			}

			return true;
		}

		public bool CanGoBack()
		{
			return mode_.History.CanGoBack();
		}

		public bool CanGoNext()
		{
			return mode_.History.CanGoNext();
		}

		public bool CanGoUp()
		{
			return tree_.CanGoUp();
		}

		public void Back()
		{
			try
			{
				ignoreHistory_ = true;
				SelectDirectory(mode_.History.Back(), false);
			}
			finally
			{
				ignoreHistory_ = false;
			}
		}

		public void Next()
		{
			try
			{
				ignoreHistory_ = true;
				SelectDirectory(mode_.History.Next(), false);
			}
			finally
			{
				ignoreHistory_ = false;
			}
		}

		public void GoHistory(int i)
		{
			try
			{
				ignoreHistory_ = true;
				if (mode_.History.SetIndex(i))
					SelectDirectory(mode_.History.Current, false);
			}
			finally
			{
				ignoreHistory_ = false;
			}
		}

		public void Up()
		{
			tree_.Up();
		}

		public void Activate(FilePanel p)
		{
			SelectFile(p.Object);
			ExecuteAction();
		}

		public void ExecuteAction()
		{
			// select the right file if the user typed a name
			SelectFromFilename();

			if (mode_.CanExecute(this))
			{
				mode_.Execute(this);
				callback_?.Invoke(mode_.GetPath(this));
				callback_ = null;
				Hide();
			}
		}

		public string GetDefaultExtension()
		{
			var e = buttonsPanel_.SelectedExtension?.Extensions[0];
			if (!string.IsNullOrEmpty(e))
				return e;

			var exts = mode_.Extensions;
			if (exts != null && exts.Length > 0 && exts[0].Extensions.Length > 0)
				return exts[0].Extensions[0];

			return ".json";
		}

		public void Cancel()
		{
			callback_?.Invoke("");
			callback_ = null;
			Hide();
		}

		private int GetTreeFlags()
		{
			int f = FileTree.NoFlags;

			if (mode_.FlattenDirectories)
				f |= FileTree.FlattenDirectories;

			if (mode_.IsWritable)
				f |= FileTree.Writeable;

			return f;
		}

		private void SetCurrentDirectory(FS.IFilesystemContainer o)
		{
			if (!ignoreHistory_)
				mode_.History.Push(o.VirtualPath);

			dir_ = o;
			addressBar_.ClearSearch();
			RefreshFiles();
		}

		protected FS.Filter CreateFilter()
		{
			return new FS.Filter(
				addressBar_.Search,
				buttonsPanel_.SelectedExtension?.Extensions,
				mode_.Sort,
				mode_.SortDirection);
		}

		public void Refresh()
		{
			FS.Filesystem.Instance.ClearCaches();
			RefreshDirectories();
			RefreshFiles();
		}

		public void RefreshFiles()
		{
			List<FS.IFilesystemObject> files;

			if (dir_.ParentPackage == null)
			{
				if (mode_.FlattenDirectories && !mode_.IsWritable)
					files = dir_.GetFilesRecursive(CreateFilter());
				else
					files = dir_.GetFiles(CreateFilter());
			}
			else
			{
				if (mode_.FlattenPackages)
					files = dir_.GetFilesRecursive(CreateFilter());
				else
					files = dir_.GetFiles(CreateFilter());
			}

			files_ = files;
			filesPanel_.SetFiles(files);
			SetPath();
			UpdateActionButton();
		}

		public void RefreshDirectories()
		{
			tree_.Refresh();
		}

		private void CreateUI()
		{
			Icons.LoadAll();

			root_ = new VUI.Root(new VUI.TransformUIRootSupport(
				SuperController.singleton.fileBrowserUI.transform.parent));

			root_.ContentPanel.Margins = new VUI.Insets(6, 0, 0, 0);
			root_.ContentPanel.Layout = new VUI.BorderLayout();

			window_ = new VUI.Window();
			window_.CloseRequest += Cancel;
			window_.ContentPanel.Layout = new VUI.BorderLayout(10);

			window_.ContentPanel.Add(CreateTop(), VUI.BorderLayout.Top);
			window_.ContentPanel.Add(CreateCenter(), VUI.BorderLayout.Center);
			window_.ContentPanel.Add(CreateBottom(), VUI.BorderLayout.Bottom);

			root_.ContentPanel.Add(window_, VUI.BorderLayout.Center);
		}

		private VUI.Panel CreateTop()
		{
			optionsPanel_ = new OptionsPanel(this);
			optionsPanel_.DirectoriesChanged += OnDirectoryOptionsChanged;
			optionsPanel_.FilesChanged += OnFileOptionsChanged;

			addressBar_ = new AddressBar(this);

			var p = new VUI.Panel(new VUI.BorderLayout(10));
			p.Add(optionsPanel_, VUI.BorderLayout.Top);
			p.Add(addressBar_, VUI.BorderLayout.Center);

			return p;
		}

		private VUI.Panel CreateCenter()
		{
			return new VUI.Splitter(
				CreateTree(),
				CreateFilesPanel(),
				VUI.Splitter.AbsolutePosition, 500);
		}

		private VUI.Panel CreateTree()
		{
			tree_ = new FileTree(this, FontSize);
			tree_.SelectionChanged += OnTreeSelection;

			var p = new VUI.Panel(new VUI.BorderLayout());
			p.Add(tree_.Widget, VUI.BorderLayout.Center);

			return p;
		}

		private VUI.Panel CreateFilesPanel()
		{
			filesPanel_ = new FilesPanel(this, Columns, Rows);
			return filesPanel_;
		}

		private VUI.Panel CreateBottom()
		{
			buttonsPanel_ = new ButtonsPanel(this);
			buttonsPanel_.FilenameChanged += UpdateActionButton;

			return buttonsPanel_;
		}

		private void UpdateActionButton()
		{
			buttonsPanel_.SetActionButton(mode_.CanExecute(this));
		}

		private void SetPath()
		{
			addressBar_.Path = dir_.VirtualPath;
		}

		private void SelectFromFilename()
		{
			if (SelectedFile == null || SelectedFile.Name != buttonsPanel_.Filename)
			{
				SelectFile(null);

				foreach (var f in files_)
				{
					if (f.Name == buttonsPanel_.Filename)
					{
						SelectFile(f);
						break;
					}
				}
			}
		}

		private void OnDirectoryOptionsChanged()
		{
			AlternateUI.Instance.Save();
			tree_.SetFlags(GetTreeFlags());
			SetCurrentDirectory(dir_);
		}

		private void OnFileOptionsChanged()
		{
			RefreshFiles();
		}

		private void OnTreeSelection(IFileTreeItem item)
		{
			var c = item?.Object as FS.IFilesystemContainer;

			SetCurrentDirectory(c);
			addressBar_.SetDirectory(c);
		}
	}
}
