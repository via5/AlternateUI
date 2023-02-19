using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections.Generic;

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

		public override string ToString()
		{
			return AUI.Path.Filename(path_);
		}
	}


	class FileDialog : BasicFeature
	{
		private const int FontSize = 24;

		private VUI.Root root_ = null;
		private IFileContainer container_ = new EmptyContainer();
		private VUI.TextBox path_ = null;
		private List<File> files_ = null;
		private FileTree tree_ = null;
		private FilePanel[] panels_ = new FilePanel[0];
		private VUI.ScrollBar sb_ = null;
		private string cwd_;
		private int top_ = 0;
		private bool flattenDirs_ = true;
		private bool flattenPackages_ = true;

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

		public void SetContainer(IFileContainer c)
		{
			container_ = c;
			top_ = 0;

			SetFiles(container_.GetFiles(this));
			SetPath();
			SetPanels(0);
		}

		protected override void DoEnable()
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

				//todo
				tree_.Select("VaM/Saves/scene/_vam/games/overwatch");
			}
		}

		private void Create()
		{
			var w = new VUI.Window("Load scene");

			w.ContentPanel.Layout = new VUI.BorderLayout(10);

			var top = new VUI.Panel(new VUI.BorderLayout(10));
			//top.Margins = new VUI.Insets(0, 0, 0, 10);

			var opts = new VUI.Panel(new VUI.HorizontalFlow(10));
			opts.Add(new VUI.CheckBox("Flatten subfolders", b => FlattenDirectories = b, flattenDirs_));
			opts.Add(new VUI.CheckBox("Flatten packages", b => FlattenPackages = b, flattenPackages_));
			top.Add(opts, VUI.BorderLayout.Top);

			path_ = top.Add(new VUI.TextBox(), VUI.BorderLayout.Center);
			path_.Submitted += OnPathSubmitted;

			w.ContentPanel.Add(top, VUI.BorderLayout.Top);
			w.ContentPanel.Add(CreateTree(), VUI.BorderLayout.Left);
			w.ContentPanel.Add(CreateFilesPanel(), VUI.BorderLayout.Center);

			root_.ContentPanel.Add(w, VUI.BorderLayout.Center);
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
			files.Padding = new VUI.Insets(10);

			panels_ = new FilePanel[Columns * Rows];

			for (int j = 0; j < Columns * Rows; ++j)
			{
				panels_[j] = new FilePanel(FontSize);
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
			filesPanel.Events.Wheel += OnWheel;

			return filesPanel;
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

		private void OnWheel(VUI.WheelEvent e)
		{
			int offscreenRows = OffscreenRows();
			int newTop = U.Clamp(top_ + (int)-e.Delta.Y, 0, offscreenRows);

			Log.Info($"{e.Delta.Y} {top_} {newTop}");
			float v = newTop * ScrollbarSize();

			if (e.Delta.Y < 0)
			{
				// down
				if (newTop == offscreenRows)
					v = sb_.Range;
			}
			else if (e.Delta.Y > 0)
			{
				//// up
				//if (v
				//	v = 0;
			}

			sb_.Value = v;

			Log.Info($"wheel {e.Delta.Y} {sb_.Value}");

			e.Bubble = false;
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

		private void OnScrollbar(float v)
		{
			int y = Math.Max(0, (int)(v / ScrollbarSize()));

			if (top_ != y)
			{
				top_ = y;
				Log.Info($"{top_}");
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

		private void SetPanels(int from)
		{
			int panelIndex = 0;
			for (int i=from; i<files_.Count; ++i)
			{
				var f = files_[i];
				var fp = panels_[panelIndex];

				fp.Set(f.Path);

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
