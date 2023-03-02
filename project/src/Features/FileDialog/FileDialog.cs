using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AUI.FileDialog
{
	class FileDialog
	{
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


		private const int FontSize = 24;

		private IFileDialogMode mode_ = new NoMode();
		private VUI.Root root_ = null;
		private VUI.Window window_ = null;
		private FS.IFilesystemContainer dir_ = new FS.NullDirectory();
		private VUI.CheckBox pin_ = null;
		private VUI.TextBox path_ = null;
		private SearchBox search_ = null;
		private VUI.TextBox filename_ = null;
		private VUI.ComboBox<ExtensionItem> extensions_;
		private VUI.Panel optionsPanel_ = null;
		private VUI.CheckBox flattenDirsButton_ = null;
		private VUI.CheckBox flattenPackagesButton_ = null;
		private VUI.MenuButton sortPanel_ = null;
		private List<FS.IFilesystemObject> files_ = null;
		private FileTree tree_ = null;
		private VUI.FixedScrolledPanel filesPanel_ = null;
		private FilePanel[] panels_ = new FilePanel[0];
		private FS.IFilesystemObject selected_ = null;
		private VUI.Button action_ = null;
		private bool flattenDirs_ = true;
		private bool flattenPackages_ = true;
		private int sort_ = FS.Filter.SortFilename;
		private int sortDir_ = FS.Filter.SortAscending;
		private bool ignoreSearch_ = false;
		private bool ignorePin_ = false;
		private Action<string> callback_ = null;
		private bool ignoreFlatten_ = false;

		public FileDialog()
		{
		}

		public int Columns
		{
			get { return 5; }
		}

		public int Rows
		{
			get { return 4; }
		}

		public FS.IFilesystemContainer CurrentDirectory
		{
			get { return dir_; }
		}

		public bool FlattenDirectories
		{
			get
			{
				return flattenDirs_;
			}

			set
			{
				if (flattenDirs_ != value)
				{
					flattenDirs_ = value;
					AlternateUI.Instance.Save();
					UpdateFlatten();
				}
			}
		}

		public bool FlattenPackages
		{
			get
			{
				return flattenPackages_;
			}

			set
			{
				if (flattenPackages_ != value)
				{
					flattenPackages_ = value;
					AlternateUI.Instance.Save();
					UpdateFlatten();
				}
			}
		}

		public FS.IFilesystemObject Selected
		{
			get { return selected_; }
		}

		public string Filename
		{
			get { return filename_.Text; }
		}

		public void Select(FS.IFilesystemObject o)
		{
			if (selected_ == o)
				return;

			if (selected_ != null)
				FindPanel(selected_)?.SetSelectedInternal(false);

			selected_ = o;
			filename_.Text = selected_?.DisplayName ?? "";

			if (selected_ != null)
				FindPanel(selected_)?.SetSelectedInternal(true);

			UpdateActionButton();
		}

		public void SetCurrentDirectory(string vpath, bool expand = true)
		{
			tree_.Select(vpath, expand);
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

		public void Enable()
		{
		}

		public void Disable()
		{
			if (root_ != null)
				root_.Visible = false;
		}

		public void Activate(FilePanel p)
		{
			Select(p.Object);
			ExecuteAction();
		}

		public string Title
		{
			get { return window_.Title; }
			set { window_.Title = value; }
		}

		public string ActionText
		{
			get { return action_.Text; }
			set { action_.Text = value; }
		}

		public ExtensionItem[] Extensions
		{
			set { extensions_.SetItems(value); }
		}

		public string GetDefaultExtension()
		{
			var e = extensions_.Selected?.Extensions[0];
			if (!string.IsNullOrEmpty(e))
				return e;

			var exts = mode_.GetExtensions();
			if (exts != null && exts.Length > 0 && exts[0].Extensions.Length > 0)
				return exts[0].Extensions[0];

			return ".json";
		}

		private void ExecuteAction()
		{
			if (Selected == null || Selected.Name != filename_.Text)
			{
				Select(null);

				foreach (var f in files_)
				{
					if (f.Name == filename_.Text)
					{
						Select(f);
						break;
					}
				}
			}

			if (mode_.CanExecute(this))
			{
				mode_.Execute(this);
				callback_?.Invoke(mode_.GetPath(this));
				callback_ = null;
				Hide();
			}
		}

		public void Cancel()
		{
			callback_?.Invoke("");
			callback_ = null;
			Hide();
		}

		public void Show(IFileDialogMode mode, Action<string> callback)
		{
			if (root_ == null)
			{
				Icons.LoadAll();

				root_ = new VUI.Root(new VUI.TransformUIRootSupport(
					SuperController.singleton.fileBrowserUI.transform.parent));

				root_.ContentPanel.Margins = new VUI.Insets(6, 0, 0, 0);
				root_.ContentPanel.Layout = new VUI.BorderLayout();

				Create();

				SetContainer(new FS.NullDirectory());
				UpdateFlatten();
			}

			mode_ = mode;
			callback_ = callback;

			root_.Visible = true;
			ClearPanels();
			flattenDirsButton_.Visible = !mode_.IsWritable();
			flattenPackagesButton_.Visible = !mode_.IsWritable();

			flattenDirs_ = mode_.GetDefaultFlattenDirectories();

			try
			{
				ignoreFlatten_ = true;
				flattenDirsButton_.Checked = flattenDirs_;
			}
			finally
			{
				ignoreFlatten_ = false;
			}

			tree_.Enable();
			Title = mode_.GetTitle();
			ActionText = mode_.GetActionText();
			Extensions = mode_.GetExtensions();
			Select(null);
			filename_.Text = selected_?.DisplayName ?? "";
			SetCurrentDirectory(mode_.GetCurrentDirectory());
			RefreshFiles();

			//UpdateActionButton();
			tree_.SetFlags(GetTreeFlags());

			filename_.Focus();
		}

		private int GetTreeFlags()
		{
			int f = FileTree.NoFlags;

			if (FlattenDirectories)
				f |= FileTree.FlattenDirectories;

			if (mode_.IsWritable())
				f |= FileTree.Writeable;

			return f;
		}

		public void Hide()
		{
			tree_.Disable();
			root_.Visible = false;
		}

		public void SetContainer(FS.IFilesystemContainer o)
		{
			dir_ = o;

			try
			{
				ignoreSearch_ = true;
				search_.Text = "";
			}
			finally
			{
				ignoreSearch_ = false;
			}

			RefreshFiles();
		}

		protected FS.Filter CreateFilter()
		{
			return new FS.Filter(search_.Text, extensions_.Selected?.Extensions, sort_, sortDir_);
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
				if (FlattenDirectories && !mode_.IsWritable())
					files = dir_.GetFilesRecursive(CreateFilter());
				else
					files = dir_.GetFiles(CreateFilter());
			}
			else
			{
				if (FlattenPackages)
					files = dir_.GetFilesRecursive(CreateFilter());
				else
					files = dir_.GetFiles(CreateFilter());
			}

			SetFiles(files);
			SetPath();
			SetPanels(0);
			UpdateActionButton();
		}

		public void RefreshDirectories()
		{
			tree_.Refresh();
		}

		private void Create()
		{
			window_ = new VUI.Window();
			window_.CloseRequest += Cancel;

			window_.ContentPanel.Layout = new VUI.BorderLayout(10);

			var sp = new VUI.Splitter(CreateTree(), CreateFilesPanel(), VUI.Splitter.AbsolutePosition, 500);

			window_.ContentPanel.Add(CreateTop(), VUI.BorderLayout.Top);
			window_.ContentPanel.Add(sp, VUI.BorderLayout.Center);
			window_.ContentPanel.Add(CreateBottom(), VUI.BorderLayout.Bottom);

			root_.ContentPanel.Add(window_, VUI.BorderLayout.Center);
		}

		private VUI.RadioMenuItem MakeSortItem(string text, int sort)
		{
			VUI.RadioButton.ChangedCallback cb = (bool b) =>
			{
				if (b)
				{
					sort_ = sort;
					RefreshFiles();
				}
			};

			return new VUI.RadioMenuItem(text, cb, (sort_ == sort), "sort");
		}

		private VUI.RadioMenuItem MakeSortDirItem(string text, int sortDir)
		{
			VUI.RadioButton.ChangedCallback cb = (bool b) =>
			{
				if (b)
				{
					sortDir_ = sortDir;
					RefreshFiles();
				}
			};

			return new VUI.RadioMenuItem(text, cb, (sortDir_ == sortDir), "sortDir");
		}

		private VUI.Panel CreateTop()
		{
			var top = new VUI.Panel(new VUI.BorderLayout(10));

			var sortMenu = new VUI.Menu();

			sortMenu.AddMenuItem(MakeSortItem("Filename", FS.Filter.SortFilename));
			sortMenu.AddMenuItem(MakeSortItem("Type", FS.Filter.SortType));
			sortMenu.AddMenuItem(MakeSortItem("Date modified", FS.Filter.SortDateModified));
			sortMenu.AddMenuItem(MakeSortItem("Date created", FS.Filter.SortDateCreated));
			sortMenu.AddSeparator();
			sortMenu.AddMenuItem(MakeSortDirItem("Ascending", FS.Filter.SortAscending));
			sortMenu.AddMenuItem(MakeSortDirItem("Descending", FS.Filter.SortDescending));

			sortPanel_ = new VUI.MenuButton("Sort", sortMenu);

			optionsPanel_ = new VUI.Panel(new VUI.HorizontalFlow(10));
			flattenDirsButton_ = optionsPanel_.Add(new VUI.CheckBox("Flatten folders", b => FlattenDirectories = b, flattenDirs_));
			flattenPackagesButton_ = optionsPanel_.Add(new VUI.CheckBox("Flatten package content", b =>
			{
				if (ignoreFlatten_) return;
				FlattenPackages = b;
			}, flattenPackages_));

			optionsPanel_.Add(sortPanel_.Button);
			optionsPanel_.Add(new VUI.Button("Refresh", Refresh));
			top.Add(optionsPanel_, VUI.BorderLayout.Top);

			var left = new VUI.Panel(new VUI.BorderLayout(10));

			path_ = new VUI.TextBox();
			path_.Submitted += OnPathSubmitted;

			pin_= left.Add(new VUI.CheckBox("Pin", OnPin), VUI.BorderLayout.Left);
			left.Add(path_, VUI.BorderLayout.Center);

			search_ = new SearchBox("Search");
			search_.MinimumSize = new VUI.Size(400, VUI.Widget.DontCare);
			search_.Changed += OnSearchChanged;

			top.Add(new VUI.Splitter(left, search_, VUI.Splitter.MinimumSecond), VUI.BorderLayout.Center);

			return top;
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
			filesPanel_ = new VUI.FixedScrolledPanel();
			var p = filesPanel_.ContentPanel;

			var gl = new VUI.GridLayout(Columns, 10);
			gl.UniformWidth = true;

			p.Layout = gl;
			p.Padding = new VUI.Insets(0, 0, 5, 0);

			panels_ = new FilePanel[Columns * Rows];

			for (int j = 0; j < Columns * Rows; ++j)
			{
				panels_[j] = new FilePanel(this, FontSize);
				p.Add(panels_[j].Panel);
			}

			filesPanel_.Scrolled += OnScroll;
			filesPanel_.Events.PointerClick += OnClicked;

			return filesPanel_;
		}

		private VUI.Panel CreateBottom()
		{
			var p = new VUI.Panel(new VUI.VerticalFlow(20));
			p.Padding = new VUI.Insets(20);
			p.Borders = new VUI.Insets(0, 1, 0, 0);

			var fn = new VUI.Panel(new VUI.BorderLayout(10));
			fn.Padding = new VUI.Insets(30, 0, 0, 0);
			fn.Add(new VUI.Label("File name:"), VUI.BorderLayout.Left);

			filename_ = fn.Add(new VUI.TextBox(), VUI.BorderLayout.Center);
			filename_.Changed += OnFilenameChanged;
			filename_.Submitted += (s) => ExecuteAction();

			extensions_ = fn.Add(new VUI.ComboBox<ExtensionItem>(), VUI.BorderLayout.Right);
			extensions_.MinimumSize = new VUI.Size(500, VUI.Widget.DontCare);
			extensions_.MaximumSize = new VUI.Size(500, VUI.Widget.DontCare);
			extensions_.PopupWidth = 500;
			extensions_.SelectionChanged += OnExtensionChanged;

			var buttons = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.FlowLayout.AlignRight | VUI.FlowLayout.AlignVCenter));
			action_ = buttons.Add(new VUI.Button("", ExecuteAction));
			buttons.Add(new VUI.Button("Cancel", Cancel));

			p.Add(fn);
			p.Add(buttons);

			return p;
		}

		private void OnScroll(int top)
		{
			SetPanels(top * Columns);
		}

		private void OnClicked(VUI.PointerEvent e)
		{
			if (e.Button == VUI.PointerEvent.LeftButton)
				Select(null);

			e.Bubble = false;
		}

		private void SetFiles(List<FS.IFilesystemObject> list)
		{
			files_ = list;
			AlternateUI.Instance.StartCoroutine(CoSetScrollPanel());
		}

		private IEnumerator CoSetScrollPanel()
		{
			yield return new WaitForEndOfFrame();

			int totalRows = (int)Math.Ceiling((float)files_.Count / Columns);
			int offscreenRows = totalRows - Rows;
			float scrollbarSize = filesPanel_.ContentPanel.ClientBounds.Height / Rows / 3;

			filesPanel_.Set(offscreenRows, scrollbarSize);
		}

		private void ClearPanels()
		{
			for (int i = 0; i < panels_.Length; ++i)
				panels_[i]?.Clear();
		}

		private void SetPanels(int from)
		{
			int panelIndex = 0;
			for (int i=from; i<files_.Count; ++i)
			{
				var f = files_[i];
				var fp = panels_[panelIndex];

				fp.Set(f);
				fp.SetSelectedInternal(selected_ == f);

				++panelIndex;
				if (panelIndex >= Columns * Rows)
					break;
			}

			while (panelIndex < Columns * Rows)
			{
				panels_[panelIndex].Clear();
				++panelIndex;
			}
		}

		private void UpdateActionButton()
		{
			action_.Enabled = mode_.CanExecute(this);
		}

		private void SetPath()
		{
			path_.Text = dir_.VirtualPath;
		}

		public void Update(float s)
		{
			root_?.Update();
		}

		private void UpdateFlatten()
		{
			tree_.SetFlags(GetTreeFlags());
			SetContainer(dir_);
		}

		public void LoadOptions(JSONClass o)
		{
			if (o.HasKey("flattenDirs"))
				flattenDirs_ = o["flattenDirs"].AsBool;

			if (o.HasKey("flattenPackages"))
				flattenPackages_ = o["flattenPackages"].AsBool;
		}

		public void SaveOptions(JSONClass o)
		{
			o["flattenDirs"] = new JSONData(flattenDirs_);
			o["flattenPackages"] = new JSONData(flattenPackages_);
		}

		private void OnPin(bool b)
		{
			if (ignorePin_) return;

			var s = tree_.Selected as FS.IFilesystemContainer;
			if (s != null)
			{
				if (b)
					FS.Filesystem.Instance.Pin(s);
				else
					FS.Filesystem.Instance.Unpin(s);
			}
		}

		private void OnPathSubmitted(string s)
		{
			if (!tree_.Select(s))
				SetPath();
		}

		private void OnFilenameChanged(string s)
		{
			UpdateActionButton();
		}

		private void OnSearchChanged(string s)
		{
			if (ignoreSearch_) return;
			RefreshFiles();
		}

		private void OnExtensionChanged(ExtensionItem e)
		{
			RefreshFiles();
		}

		private void OnTreeSelection(IFileTreeItem item)
		{
			try
			{
				ignorePin_ = true;
				var c = item?.Object as FS.IFilesystemContainer;

				if (c != null)
				{
					SetContainer(c);
					pin_.Enabled = c.CanPin;
					pin_.Checked = FS.Filesystem.Instance.IsPinned(c);
				}
				else
				{
					SetContainer(new FS.NullDirectory());
					pin_.Enabled = false;
					pin_.Checked = false;
				}
			}
			finally
			{
				ignorePin_ = false;
			}
		}
	}
}
