using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

namespace AUI.FileDialog
{
	class File
	{
		private readonly string path_;

		public File(string path)
		{
			path_ = path.Replace('\\', '/');
		}

		public string Path
		{
			get { return path_; }
		}

		public string Filename
		{
			get { return AUI.Path.Filename(path_); }
		}

		public override string ToString()
		{
			return AUI.Path.Filename(path_);
		}
	}


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

		private const int FontSize = 24;

		public const int NoType = 0;
		public const int OpenScene = 1;
		public const int SaveScene = 2;

		private int type_ = NoType;
		private VUI.Root root_ = null;
		private VUI.Window window_ = null;
		private IFileContainer container_ = new EmptyContainer();
		private VUI.TextBox path_ = null;
		private SearchBox search_ = null;
		private VUI.TextBox filename_ = null;
		private VUI.ComboBox<ExtensionItem> extensions_;
		private VUI.Panel optionsPanel_ = null;
		private List<File> files_ = null;
		private FileTree tree_ = null;
		private FilePanel[] panels_ = new FilePanel[0];
		private FilePanel selected_ = null;
		private VUI.Button action_ = null;
		private VUI.ScrollBar sb_ = null;
		private string cwd_;
		private int top_ = 0;
		private bool flattenDirs_ = true;
		private bool flattenPackages_ = true;
		private readonly List<ReplacedButton> replacedButtons_ = new List<ReplacedButton>();
		private bool ignoreSearch_ = false;

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

		public void Select(FilePanel p)
		{
			if (selected_ == p)
				return;

			if (selected_ != null)
				selected_.SetSelectedInternal(false);

			selected_ = p;
			filename_.Text = selected_?.File?.Filename ?? "";

			if (selected_ != null)
				selected_.SetSelectedInternal(true);

			UpdateActionButton();
		}

		public void Activate(FilePanel p)
		{
			Select(p);
			ExecuteAction();
		}

		public void Open()
		{
			if (!CanOpen())
				return;

			Log.Info($"open {selected_.File.Path}");

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
			if (container_ == null || container_.Virtual)
				return "";

			var dir = container_?.Path?.Trim() ?? "";
			if (dir == "")
				return "";

			var file = filename_.Text.Trim();
			if (file == "")
				return "";

			if (file.IndexOf('.') == -1)
			{
				var e = extensions_.Selected?.Extensions[0] ?? Cache.DefaultSceneExtension;
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

				SetContainer(new EmptyContainer());
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
					tree_.Set(false);
					tree_.FlattenDirectories = FlattenDirectories;
					break;
				}

				case SaveScene:
				{
					window_.Title = "Save scene";
					action_.Text = "Save";
					optionsPanel_.Visible = false;
					extensions_.SetItems(GetSaveSceneExtensions());
					tree_.FlattenDirectories = false;
					tree_.Set(true);
					break;
				}
			}

			root_.Visible = true;
			UpdateActionButton();
		}

		public void Hide()
		{
			root_.Visible = false;
		}

		private void SetContainer(IFileContainer c)
		{
			container_ = c;

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

		private void Refresh()
		{
			top_ = 0;
			container_.Search = search_.Text;
			container_.Extensions = extensions_.Selected?.Extensions;

			SetFiles(container_.GetFiles(this));
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

		private VUI.Panel CreateTop()
		{
			var top = new VUI.Panel(new VUI.BorderLayout(10));

			optionsPanel_ = new VUI.Panel(new VUI.HorizontalFlow(10));
			optionsPanel_.Add(new VUI.CheckBox("Flatten folders", b => FlattenDirectories = b, flattenDirs_));
			optionsPanel_.Add(new VUI.CheckBox("Flatten package content", b => FlattenPackages = b, flattenPackages_));
			top.Add(optionsPanel_, VUI.BorderLayout.Top);

			path_ = new VUI.TextBox();
			path_.Submitted += OnPathSubmitted;

			search_ = new SearchBox("Search");
			search_.MinimumSize = new VUI.Size(400, VUI.Widget.DontCare);
			search_.Changed += OnSearchChanged;

			top.Add(new VUI.Splitter(path_, search_, VUI.Splitter.MinimumSecond), VUI.BorderLayout.Center);

			return top;
		}

		private VUI.Panel CreateTree()
		{
			tree_ = new FileTree(FontSize);

			tree_.PathSelected += OnPathSelected;
			tree_.PackageSelected += OnPackageSelected;
			tree_.AllFlatSelected += OnAllFlat;
			tree_.PackagesFlatSelected += OnPackagesFlat;
			tree_.NothingSelected += OnNothingSelected;

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
			var p = new VUI.Panel(new VUI.VerticalFlow(10));
			p.Padding = new VUI.Insets(20);

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

		private void SetFiles(List<File> list)
		{
			U.NatSort(list);
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
			path_.Text = "VaM/" + container_.Path.Replace('\\', '/');
		}

		protected override void DoUpdate(float s)
		{
			root_?.Update();
		}

		private void UpdateFlatten()
		{
			tree_.FlattenDirectories = flattenDirs_;
			SetContainer(container_);
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

		private void OnNothingSelected()
		{
			if (!(container_ is EmptyContainer))
				SetContainer(new EmptyContainer());
		}

		private void OnPathSelected(string p)
		{
			SetContainer(new DirectoryContainer(p));
		}

		private void OnPackageSelected(ShortCut sc)
		{
			SetContainer(new PackageContainer(sc));
		}

		private void OnAllFlat()
		{
			SetContainer(new AllFlatContainer());
		}

		private void OnPackagesFlat()
		{
			SetContainer(new PackagesFlatContainer());
		}

		private List<ExtensionItem> GetOpenSceneExtensions()
		{
			var list = new List<ExtensionItem>();

			string all = "";
			var allExts = new List<string>();

			foreach (var e in Cache.SceneExtensions)
			{
				if (all != "")
					all += "; ";

				all += "*" + e.ext;
				allExts.Add(e.ext);
			}

			all = "All scene files (" + all + ")";
			list.Add(new ExtensionItem(all, allExts.ToArray()));

			foreach (var e in Cache.SceneExtensions)
				list.Add(new ExtensionItem(e.name + " (*" + e.ext + ")", new string[] { e.ext }));

			return list;
		}

		private List<ExtensionItem> GetSaveSceneExtensions()
		{
			var list = new List<ExtensionItem>();

			foreach (var e in Cache.SceneExtensions)
				list.Add(new ExtensionItem(e.name + " (*" + e.ext + ")", new string[] { e.ext }));

			//list.Add(new ExtensionItem(Cache.DefaultSceneExtension, new string[] { Cache.DefaultSceneExtension }));
			return list;
		}
	}
}
