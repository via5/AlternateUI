using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace AUI.FileDialog
{
	class FileDialog : BasicFeature
	{
		class ReplacedButton
		{
			public Button b;
			public Button.ButtonClickedEvent oldEvent;
		}

		class ExtensionItem
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

		class SortItem
		{
		}


		private const int FontSize = 24;

		public const int NoType = 0;
		public const int OpenScene = 1;
		public const int SaveScene = 2;

		private int type_ = NoType;
		private VUI.Root root_ = null;
		private VUI.Window window_ = null;
		private IFilesystemContainer dir_ = new NullDirectory();
		private VUI.CheckBox pin_ = null;
		private VUI.TextBox path_ = null;
		private SearchBox search_ = null;
		private VUI.TextBox filename_ = null;
		private VUI.ComboBox<ExtensionItem> extensions_;
		private VUI.Panel optionsPanel_ = null;
		private VUI.MenuButton sortPanel_ = null;
		private List<IFilesystemObject> files_ = null;
		private FileTree tree_ = null;
		private FilePanel[] panels_ = new FilePanel[0];
		private IFilesystemObject selected_ = null;
		private VUI.Button action_ = null;
		private VUI.ScrollBar sb_ = null;
		private string cwd_;
		private int top_ = 0;
		private bool flattenDirs_ = true;
		private bool flattenPackages_ = true;
		private int sort_ = Filter.SortFilename;
		private int sortDir_ = Filter.SortAscending;
		private readonly List<ReplacedButton> replacedButtons_ = new List<ReplacedButton>();
		private bool ignoreSearch_ = false;
		private bool ignorePin_ = false;

		public FileDialog()
			: base("fileDialog", "File dialog", false)
		{
			cwd_ = null;
		}

		public override string Description
		{
			get { return "Replaces the file dialogs."; }
		}

		public int Columns
		{
			get { return 5; }
		}

		public int Rows
		{
			get { return 4; }
		}

		public string CurrentDirectory
		{
			get { return cwd_; }
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

		public IFilesystemObject Selected
		{
			get { return selected_; }
		}

		public void Select(IFilesystemObject o)
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

		private FilePanel FindPanel(IFilesystemObject o)
		{
			for (int i = 0; i < panels_.Length; ++i)
			{
				if (panels_[i].Object == o)
					return panels_[i];
			}

			return null;
		}

		public void Activate(FilePanel p)
		{
			Select(p.Object);
			ExecuteAction();
		}

		public void Open()
		{
			if (!CanOpen())
				return;

			Log.Info($"open {selected_.VirtualPath}");

			Hide();
			//SuperController.singleton.Load(selected_.File.Path);
		}

		public void Save()
		{
			var path = GetSavePath();
			if (path == "")
				return;

			Log.Info($"saving {path}");

			Hide();
			//SuperController.singleton.Save(path);
		}

		public bool CanOpen()
		{
			return (selected_ != null);
		}

		private bool CanSave()
		{
			return (GetSavePath() != "");
		}

		private string GetSavePath()
		{
			if (dir_ == null || dir_.Virtual)
				return "";

			var dir = dir_.VirtualPath?.Trim() ?? "";
			if (dir == "")
				return "";

			var file = filename_.Text.Trim();
			if (file == "")
				return "";

			if (file.IndexOf('.') == -1)
			{
				var e = extensions_.Selected?.Extensions[0] ?? Filesystem.DefaultSceneExtension;
				file += e;
			}

			return Path.Join(dir, file);
		}

		private void ExecuteAction()
		{
			switch (type_)
			{
				case OpenScene:
				{
					Open();
					break;
				}

				case SaveScene:
				{
					Save();
					break;
				}
			}
		}

		public void Cancel()
		{
			Hide();
		}

		public void Show(int type)
		{
			if (root_ == null)
			{
				Icons.LoadAll();

				root_ = new VUI.Root(new VUI.TransformUIRootSupport(
					SuperController.singleton.fileBrowserUI.transform.parent));

				root_.ContentPanel.Margins = new VUI.Insets(6, 0, 0, 0);
				root_.ContentPanel.Layout = new VUI.BorderLayout();

				Create();

				SetContainer(new NullDirectory());
				UpdateFlatten();
			}

			type_ = type;

			switch (type_)
			{
				case OpenScene:
				{
					window_.Title = "Load scene";
					action_.Text = "Open";
					optionsPanel_.Visible = true;
					extensions_.SetItems(GetOpenSceneExtensions());
					tree_.SetFlags(GetTreeFlags());
					break;
				}

				case SaveScene:
				{
					window_.Title = "Save scene";
					action_.Text = "Save";
					optionsPanel_.Visible = false;
					extensions_.SetItems(GetSaveSceneExtensions());
					tree_.SetFlags(GetTreeFlags());
					break;
				}
			}

			root_.Visible = true;
			tree_.Enable();
			UpdateActionButton();
		}

		private int GetTreeFlags()
		{
			int f = FileTree.NoFlags;

			if (FlattenDirectories)
				f |= FileTree.FlattenDirectories;

			if (type_ == SaveScene)
				f |= FileTree.Writeable;

			return f;
		}

		public void Hide()
		{
			tree_.Disable();
			root_.Visible = false;
		}

		public void SetContainer(IFilesystemContainer o)
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

			Refresh();
		}

		protected Filter CreateFilter()
		{
			return new Filter(search_.Text, extensions_.Selected?.Extensions, sort_, sortDir_);
		}

		private void Refresh()
		{
			top_ = 0;

			List<IFilesystemObject> files;

			if (FlattenDirectories)
				files = dir_.GetFilesRecursive(CreateFilter());
			else
				files = dir_.GetFiles(CreateFilter());

			SetFiles(files);
			SetPath();
			SetPanels(0);
			UpdateActionButton();
		}

		protected override void DoEnable()
		{
			ReplaceButton("ButtonOpenScene", OpenScene);
			ReplaceButton("ButtonSaveScene", SaveScene);

			Show(OpenScene);
		}

		protected override void DoDisable()
		{
			RestoreButtons();
		}

		private void ReplaceButton(string name, int type)
		{
			var rb = new ReplacedButton();

			rb.b = VUI.Utilities.FindChildRecursive(
				SuperController.singleton.transform, name)
					?.GetComponent<Button>();

			rb.oldEvent = rb.b.onClick;
			rb.b.onClick = new Button.ButtonClickedEvent();
			rb.b.onClick.AddListener(() => Show(type));

			replacedButtons_.Add(rb);

			Log.Verbose($"replacing button {name} for type {type}");
		}

		private void RestoreButtons()
		{
			foreach (var rb in replacedButtons_)
			{
				rb.b.onClick = rb.oldEvent;
				Log.Verbose($"restored button {Name}");
			}

			replacedButtons_.Clear();
		}

		private void Create()
		{
			window_ = new VUI.Window();

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
					Refresh();
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
					Refresh();
				}
			};

			return new VUI.RadioMenuItem(text, cb, (sortDir_ == sortDir), "sortDir");
		}

		private VUI.Panel CreateTop()
		{
			var top = new VUI.Panel(new VUI.BorderLayout(10));

			var sortMenu = new VUI.Menu();

			sortMenu.AddMenuItem(MakeSortItem("Filename", Filter.SortFilename));
			sortMenu.AddMenuItem(MakeSortItem("Type", Filter.SortType));
			sortMenu.AddMenuItem(MakeSortItem("Date modified", Filter.SortDateModified));
			sortMenu.AddMenuItem(MakeSortItem("Date created", Filter.SortDateCreated));
			sortMenu.AddSeparator();
			sortMenu.AddMenuItem(MakeSortDirItem("Ascending", Filter.SortAscending));
			sortMenu.AddMenuItem(MakeSortDirItem("Descending", Filter.SortDescending));

			sortPanel_ = new VUI.MenuButton("Sort", sortMenu);

			optionsPanel_ = new VUI.Panel(new VUI.HorizontalFlow(10));
			optionsPanel_.Add(new VUI.CheckBox("Flatten folders", b => FlattenDirectories = b, flattenDirs_));
			optionsPanel_.Add(new VUI.CheckBox("Flatten package content", b => FlattenPackages = b, flattenPackages_));
			optionsPanel_.Add(sortPanel_.Button);
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
			var gl = new VUI.GridLayout(Columns, 10);
			gl.UniformWidth = true;

			var files = new VUI.Panel(gl);
			files.Padding = new VUI.Insets(0);

			panels_ = new FilePanel[Columns * Rows];

			for (int j = 0; j < Columns * Rows; ++j)
			{
				panels_[j] = new FilePanel(this, FontSize);
				files.Add(panels_[j].Panel);
			}

			sb_ = new VUI.ScrollBar();
			sb_.MinimumSize = new VUI.Size(VUI.Style.Metrics.ScrollBarWidth, 0);
			sb_.ValueChanged += OnScrollbar;
			sb_.DragEnded += OnScrollbarDragEnded;

			var filesPanel = new VUI.Panel(new VUI.BorderLayout());
			filesPanel.Add(files, VUI.BorderLayout.Center);
			filesPanel.Add(sb_, VUI.BorderLayout.Right);

			filesPanel.MinimumSize = new VUI.Size(300, VUI.Widget.DontCare);
			filesPanel.Margins = new VUI.Insets(0);
			filesPanel.Padding = new VUI.Insets(5, 0, 0, 0);
			filesPanel.Borders = new VUI.Insets(0);

			filesPanel.Clickthrough = false;
			filesPanel.Events.PointerClick += OnClicked;
			filesPanel.Events.Wheel += OnWheel;

			return filesPanel;
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
			extensions_.MinimumSize = new VUI.Size(400, VUI.Widget.DontCare);
			extensions_.MaximumSize = new VUI.Size(400, VUI.Widget.DontCare);
			extensions_.PopupWidth = 500;
			extensions_.SelectionChanged += OnExtensionChanged;

			var buttons = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.FlowLayout.AlignRight | VUI.FlowLayout.AlignVCenter));
			action_ = buttons.Add(new VUI.Button("", ExecuteAction));
			buttons.Add(new VUI.Button("Cancel", Cancel));

			p.Add(fn);
			p.Add(buttons);

			return p;
		}

		private float ScrollbarSize()
		{
			return root_.ContentPanel.ClientBounds.Height / Rows / 5;
		}

		private int OffscreenRows()
		{
			int totalRows = (int)Math.Round((float)files_.Count / Columns);
			return totalRows - Rows;
		}

		private void OnClicked(VUI.PointerEvent e)
		{
			if (e.Button == VUI.PointerEvent.LeftButton)
				Select(null);

			e.Bubble = false;
		}

		private void OnWheel(VUI.WheelEvent e)
		{
			int offscreenRows = OffscreenRows();
			int newTop = U.Clamp(top_ + (int)-e.Delta.Y, 0, offscreenRows);
			float v = newTop * ScrollbarSize();

			if (e.Delta.Y < 0)
			{
				// down
				if (newTop == offscreenRows)
					v = sb_.Range;
			}

			sb_.Value = v;

			e.Bubble = false;
		}

		private void OnScrollbar(float v)
		{
			int y = Math.Max(0, (int)Math.Round(v / ScrollbarSize()));

			if (top_ != y)
			{
				top_ = y;
				SetPanels(top_ * Columns);
			}
		}

		private void OnScrollbarDragEnded()
		{
			if (top_ >= OffscreenRows())
				sb_.Value = sb_.Range;
			else
				sb_.Value = top_ * ScrollbarSize();
		}

		private void SetFiles(List<IFilesystemObject> list)
		{
			files_ = list;

			if (root_.Bounds.Height <= 0)
			{
				sb_.Range = 0;
			}
			else
			{
				int offscreenRows = OffscreenRows();

				if (offscreenRows <= 0)
				{
					sb_.Range = 0;
					sb_.Enabled = false;
				}
				else
				{
					sb_.Range = (offscreenRows + 1) * ScrollbarSize() - 1;
					sb_.Enabled = true;
				}
			}
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
			switch (type_)
			{
				case OpenScene:
				{
					action_.Enabled = CanOpen();
					break;
				}

				case SaveScene:
				{
					action_.Enabled = CanSave();
					break;
				}
			}
		}

		private void SetPath()
		{
			path_.Text = dir_.VirtualPath;
		}

		protected override void DoUpdate(float s)
		{
			root_?.Update();
		}

		private void UpdateFlatten()
		{
			tree_.SetFlags(GetTreeFlags());
			SetContainer(dir_);
		}

		protected override void DoLoadOptions(JSONClass o)
		{
			if (o.HasKey("flattenDirs"))
				flattenDirs_ = o["flattenDirs"].AsBool;

			if (o.HasKey("flattenPackages"))
				flattenPackages_ = o["flattenPackages"].AsBool;
		}

		protected override void DoSaveOptions(JSONClass o)
		{
			o["flattenDirs"] = new JSONData(flattenDirs_);
			o["flattenPackages"] = new JSONData(flattenPackages_);
		}

		private void OnPin(bool b)
		{
			if (ignorePin_) return;

			var s = tree_.Selected as IFilesystemContainer;
			if (s != null)
			{
				if (b)
					FS.Instance.Pin(s);
				else
					FS.Instance.Unpin(s);
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
			Refresh();
		}

		private void OnExtensionChanged(ExtensionItem e)
		{
			Refresh();
		}

		private void OnTreeSelection(IFileTreeItem item)
		{
			try
			{
				ignorePin_ = true;
				var c = item?.Object as IFilesystemContainer;

				if (c != null)
				{
					pin_.Enabled = true;
					pin_.Checked = FS.Instance.IsPinned(c);
				}
				else
				{
					pin_.Enabled = false;
					pin_.Checked = false;
				}
			}
			finally
			{
				ignorePin_ = false;
			}
		}

		private void OnSortFilename(bool b)
		{
			if (b)
			{
				sort_ = Filter.SortFilename;
				Refresh();
			}
		}

		private void OnSortType(bool b)
		{
			if (b)
			{
				sort_ = Filter.SortType;
				Refresh();
			}
		}

		private void OnSortModified(bool b)
		{
			if (b)
			{
				sort_ = Filter.SortDateModified;
				Refresh();
			}
		}

		private void OnSortCreated(bool b)
		{
			if (b)
			{
				sort_ = Filter.SortDateCreated;
				Refresh();
			}
		}

		private void OnSortAscending(bool b)
		{
			if (b)
			{
				sortDir_ = Filter.SortAscending;
				Refresh();
			}
		}

		private void OnSortDescending(bool b)
		{
			if (b)
			{
				sortDir_ = Filter.SortDescending;
				Refresh();
			}
		}

		private List<ExtensionItem> GetOpenSceneExtensions()
		{
			var list = new List<ExtensionItem>();

			string all = "";
			var allExts = new List<string>();

			foreach (var e in Filesystem.SceneExtensions)
			{
				if (all != "")
					all += "; ";

				all += "*" + e.ext;
				allExts.Add(e.ext);
			}

			all = "All scene files (" + all + ")";
			list.Add(new ExtensionItem(all, allExts.ToArray()));

			foreach (var e in Filesystem.SceneExtensions)
				list.Add(new ExtensionItem(e.name + " (*" + e.ext + ")", new string[] { e.ext }));

			return list;
		}

		private List<ExtensionItem> GetSaveSceneExtensions()
		{
			var list = new List<ExtensionItem>();

			foreach (var e in Filesystem.SceneExtensions)
				list.Add(new ExtensionItem(e.name + " (*" + e.ext + ")", new string[] { e.ext }));

			return list;
		}
	}
}
