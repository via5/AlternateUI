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
			path_ = path;
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

		private const int FontSize = 24;

		public const int OpenScene = 1;
		public const int SaveScene = 2;

		private VUI.Root root_ = null;
		private VUI.Window window_ = null;
		private IFileContainer container_ = new EmptyContainer();
		private VUI.TextBox path_ = null;
		private VUI.TextBox filename_ = null;
		private List<File> files_ = null;
		private FileTree tree_ = null;
		private FilePanel[] panels_ = new FilePanel[0];
		private FilePanel selected_ = null;
		private VUI.ScrollBar sb_ = null;
		private string cwd_;
		private int top_ = 0;
		private bool flattenDirs_ = true;
		private bool flattenPackages_ = true;
		private readonly List<ReplacedButton> replacedButtons_ = new List<ReplacedButton>();

		public FileDialog()
			: base("fileDialog", "File dialog", false)
		{
			cwd_ = null;
			LoadOptions();
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
					SaveOptions();
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
					SaveOptions();
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
		}

		public void Open()
		{
			if (selected_ == null)
				return;

			Log.Info($"open {selected_.File.Path}");
			SuperController.singleton.Load(selected_.File.Path);
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

				tree_.Rebuild();
				SetContainer(new EmptyContainer());
				UpdateFlatten();
			}

			switch (type)
			{
				case OpenScene:
				{
					window_.Title = "Load scene";
					break;
				}

				case SaveScene:
				{
					window_.Title = "Save scene";
					break;
				}
			}

			root_.Visible = true;
		}

		public void Hide()
		{
			root_.Visible = false;
		}

		private void SetContainer(IFileContainer c)
		{
			container_ = c;
			top_ = 0;

			SetFiles(container_.GetFiles(this));
			SetPath();
			SetPanels(0);
		}

		protected override void DoEnable()
		{
			ReplaceButton("ButtonOpenScene", OpenScene);
			ReplaceButton("ButtonSaveScene", SaveScene);
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

			Log.Info($"replacing button {name} for type {type}");
		}

		private void RestoreButtons()
		{
			foreach (var rb in replacedButtons_)
			{
				rb.b.onClick = rb.oldEvent;
				Log.Info($"restored button {Name}");
			}

			replacedButtons_.Clear();
		}

		private void Create()
		{
			window_ = new VUI.Window();

			window_.ContentPanel.Layout = new VUI.BorderLayout(10);

			window_.ContentPanel.Add(CreateTop(), VUI.BorderLayout.Top);
			window_.ContentPanel.Add(CreateTree(), VUI.BorderLayout.Left);
			window_.ContentPanel.Add(CreateFilesPanel(), VUI.BorderLayout.Center);
			window_.ContentPanel.Add(CreateBottom(), VUI.BorderLayout.Bottom);

			root_.ContentPanel.Add(window_, VUI.BorderLayout.Center);
		}

		private VUI.Panel CreateTop()
		{
			var top = new VUI.Panel(new VUI.BorderLayout(10));

			var opts = new VUI.Panel(new VUI.HorizontalFlow(10));
			opts.Add(new VUI.CheckBox("Flatten subfolders", b => FlattenDirectories = b, flattenDirs_));
			opts.Add(new VUI.CheckBox("Flatten packages", b => FlattenPackages = b, flattenPackages_));
			top.Add(opts, VUI.BorderLayout.Top);

			path_ = top.Add(new VUI.TextBox(), VUI.BorderLayout.Center);
			path_.Submitted += OnPathSubmitted;

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

			filesPanel.Margins = new VUI.Insets(10, 0, 0, 0);
			filesPanel.Borders = new VUI.Insets(1);

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

			var buttons = new VUI.Panel(new VUI.HorizontalFlow(10, VUI.FlowLayout.AlignRight | VUI.FlowLayout.AlignVCenter));
			buttons.Add(new VUI.Button("Open", Open));
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
			int y = Math.Max(0, (int)(v / ScrollbarSize()));

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

		private string GetOptionsFile()
		{
			return AlternateUI.Instance.GetConfigFilePath(
				$"aui.filedialog.json");
		}

		private void LoadOptions()
		{
			var f = GetOptionsFile();

			if (FileManagerSecure.FileExists(f))
			{
				var j = SuperController.singleton.LoadJSON(f)?.AsObject;
				if (j == null)
					return;

				if (j.HasKey("flattenDirs"))
					flattenDirs_ = j["flattenDirs"].AsBool;

				if (j.HasKey("flattenPackages"))
					flattenPackages_ = j["flattenPackages"].AsBool;
			}
		}

		private void SaveOptions()
		{
			var j = new JSONClass();

			j["flattenDirs"] = new JSONData(flattenDirs_);
			j["flattenPackages"] = new JSONData(flattenPackages_);

			SuperController.singleton.SaveJSON(j, GetOptionsFile());
		}

		private void OnPathSubmitted(string s)
		{
			if (!tree_.Select(s))
				SetPath();
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

		public override string Description
		{
			get { return "Replaces the file dialogs."; }
		}
	}
}
