using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AUI.FileDialog
{
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
			//FS.Instrumentation.Instance.Enabled = true;
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

			if (FS.Instrumentation.Instance.Enabled)
				DumpInstrumentation(s);
		}

		private void DumpInstrumentation(float s)
		{
			FS.Instrumentation.Instance.UpdateTickers(s);

			if (FS.Instrumentation.Instance.Updated)
			{
				Log.Info("times:");
				int longestLabel = 0;

				foreach (var i in FS.InstrumentationType.Values)
				{
					string label =
						new string(' ', FS.Instrumentation.Instance.Depth(i)) +
						FS.Instrumentation.Instance.Name(i) + " ";

					longestLabel = Math.Max(longestLabel, label.Length);
				}

				foreach (var i in FS.InstrumentationType.Values)
				{
					string label =
						new string(' ', FS.Instrumentation.Instance.Depth(i)) +
						FS.Instrumentation.Instance.Name(i) + " ";

					label = label.PadRight(longestLabel, ' ');

					Log.Info($"{label}{FS.Instrumentation.Instance.Get(i)}");
				}

				FS.Instrumentation.Reset();
			}
		}


		public Logger Log
		{
			get { return log_; }
		}

		public int Columns
		{
			get { return 4; }
		}

		public int Rows
		{
			get { return 4; }
		}

		public VUI.Root Root
		{
			get { return root_; }
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
			get { return mode_.Options.History; }
		}


		public void Show(IFileDialogMode mode, Action<string> callback, string cwd = null)
		{
			bool refreshDirs = (mode_ != mode);
			mode_ = mode;

			if (root_ == null)
				CreateUI();

			callback_ = callback;

			root_.Visible = true;
			window_.Title = mode_.Title;

			dir_ = null;
			filesPanel_.Clear();
			optionsPanel_.Set(mode_);
			buttonsPanel_.Set(mode_);

			tree_.Enable();

			if (refreshDirs)
			{
				RefreshDirectories();
			}
			else
			{
				FS.Instrumentation.Start(FS.I.FDTreeSetFlags);
				{
					tree_.SetFlags(GetTreeFlags());
				}
				FS.Instrumentation.End();
			}

			SelectInitialDirectory(cwd);

			if (dir_ == null)
			{
				// SelectInitialDirectory() won't fire the selected event if the
				// node was already selected, so call it manually to make sure
				SetCurrentDirectory(tree_.Selected as FS.IFilesystemContainer);
			}

			SelectFile(null);

			buttonsPanel_.FocusFilename();
		}

		private void SelectInitialDirectory(string cwd)
		{
			var scrollTo = VUI.TreeView.ScrollToCenter;
			var opts = mode_.Options;

			if (cwd != null)
			{
				if (SelectDirectory(cwd, false, scrollTo))
				{
					opts.CurrentDirectory = cwd;
					return;
				}

				Log.Error($"(1) bad initial directory {cwd}");
			}

			if (SelectDirectory(opts.CurrentDirectory, false, scrollTo))
				return;

			Log.Error($"(2) bad initial directory {opts.CurrentDirectory}");

			if (SelectDirectory(mode_.DefaultDirectory, false, scrollTo))
			{
				opts.CurrentDirectory = mode_.DefaultDirectory;
				return;
			}

			Log.Error($"(3) bad initial directory {mode_.DefaultDirectory}");
			opts.CurrentDirectory = FS.Filesystem.Instance.GetRoot().VirtualPath;

			if (SelectDirectory(opts.CurrentDirectory, false, scrollTo))
				return;

			Log.Error($"can't select any initial directory");
		}

		public void Hide()
		{
			tree_.Disable();
			root_.Visible = false;
		}

		public void SelectFile(FS.IFilesystemObject o, bool replaceFilename = true)
		{
			if (selected_ == o)
			{
				if (replaceFilename)
					UpdateFilename();
			}
			else
			{
				if (selected_ != null)
					filesPanel_.SetSelected(selected_, false);

				selected_ = o;

				if (replaceFilename)
					UpdateFilename();

				if (selected_ != null)
					filesPanel_.SetSelected(selected_, true);

				UpdateActionButton();
			}
		}

		private void UpdateFilename()
		{
			if (selected_ != null)
				buttonsPanel_.Filename = selected_.DisplayName;
		}

		public bool SelectDirectory(
			string vpath, bool expand = true,
			int scrollTo = VUI.TreeView.ScrollToNearest)
		{
			bool b;

			FS.Instrumentation.Start(FS.I.FDTreeSelect);
			{
				b = tree_.Select(vpath, expand, scrollTo);
			}
			FS.Instrumentation.End();

			if (!b)
			{
				SetPath();
				return false;
			}

			return true;
		}

		public bool CanGoBack()
		{
			return History.CanGoBack();
		}

		public bool CanGoNext()
		{
			return History.CanGoNext();
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
				SelectDirectory(History.Back(), false);
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
				SelectDirectory(History.Next(), false);
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
				if (History.SetIndex(i))
					SelectDirectory(History.Current, false);
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
				mode_.Execute(this, () =>
				{
					AlternateUI.Instance.StartCoroutine(
						CoRunCallback(callback_, mode_.GetPath(this)));

					callback_ = null;
					Hide();
				});
			}
		}

		private IEnumerator CoRunCallback(Action<string> f, string path)
		{
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			f?.Invoke(path);
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

			if (mode_.Options.FlattenDirectories)
				f |= FileTree.FlattenDirectories;

			if (mode_.IsWritable)
				f |= FileTree.Writeable;

			return f;
		}

		private void SetCurrentDirectory(FS.IFilesystemContainer o)
		{
			if (!ignoreHistory_)
				History.Push(o.VirtualPath);

			dir_ = o;
			addressBar_.ClearSearch();
			RefreshFiles();
		}

		private int MakeContextFlags(bool recursive, Options opts)
		{
			int f = FS.Context.NoFlags;

			if (recursive)
				f |= FS.Context.RecursiveFlag;

			if (opts.ShowHiddenFolders)
				f |= FS.Context.ShowHiddenFoldersFlag;

			if (opts.ShowHiddenFiles)
				f |= FS.Context.ShowHiddenFilesFlag;

			if (opts.MergePackages)
				f |= FS.Context.MergePackagesFlag;

			return f;
		}

		public FS.Context CreateFileContext(bool recursive)
		{
			return new FS.Context(
				addressBar_.Search,
				buttonsPanel_.SelectedExtension?.Extensions,
				mode_.PackageRoot,
				mode_.Options.Sort, mode_.Options.SortDirection,
				MakeContextFlags(recursive, mode_.Options));
		}

		public FS.Context CreateTreeContext(bool recursive)
		{
			return new FS.Context(
				"", buttonsPanel_.SelectedExtension?.Extensions,
				mode_.PackageRoot,
				FS.Context.SortFilename, FS.Context.SortAscending,
				MakeContextFlags(recursive, mode_.Options));
		}

		public void Refresh()
		{
			FS.Filesystem.Instance.ClearCaches();

			// give some time for the panels to clear so there's a visual
			// feedback that a refresh has occured
			filesPanel_.Clear();
			AlternateUI.Instance.StartCoroutine(CoRefresh());
		}

		public void RefreshBoth()
		{
			RefreshDirectories();
			RefreshFiles();
		}

		public void RefreshFiles()
		{
			FS.Instrumentation.Start(FS.I.FDGetFiles);
			{
				files_ = GetFiles();
			}
			FS.Instrumentation.End();

			FS.Instrumentation.Start(FS.I.FDSetFiles);
			{
				filesPanel_.SetFiles(files_);
			}
			FS.Instrumentation.End();

			SetPath();
			UpdateActionButton();
		}

		public void RefreshDirectories()
		{
			FS.Instrumentation.Start(FS.I.FDTreeSetFlags);
			{
				tree_.SetFlags(GetTreeFlags());
			}
			FS.Instrumentation.End();

			FS.Instrumentation.Start(FS.I.FDTreeRefresh);
			{
				tree_.Refresh();
			}
			FS.Instrumentation.End();

			var s = tree_.Selected as FS.IFilesystemContainer;
			if (s != null)
				dir_ = s;
		}

		private IEnumerator CoRefresh()
		{
			yield return new WaitForEndOfFrame();
			RefreshBoth();
		}

		private bool IsInPackage()
		{
			// can't just check for ParentPackage because this may be a merged
			// directory, which should obey FlattenDirectories, not
			// FlattenPackages

			// note: that's disabled for now, pinned packages are treated as
			// directories, not sure which one makes more sense

			var item = tree_.TreeView.Selected as FileTreeItem;

			{
				// first, check if it's a child of the packages root node

				var p = item;
				while (p != null)
				{
					var po = p.Object;
					if (po.IsSameObject(FS.Filesystem.Instance.GetPackagesRoot()))
						return true;

					p = p.Parent as FileTreeItem;
				}
			}
			/*
			{
				// then check if it's a pinned object

				var p = item;
				while (p != null)
				{
					var po = p.Object;
					if (po.IsSameObject(FS.Filesystem.Instance.GetPinnedRoot()))
						return true;

					p = p.Parent as FileTreeItem;
				}
			}*/

			return false;
		}

		private List<FS.IFilesystemObject> GetFiles()
		{
			List<FS.IFilesystemObject> files = null;

			if (IsInPackage())
			{
				var cx = CreateFileContext(mode_.Options.FlattenPackages);
				files = dir_.GetFiles(cx);
			}
			else
			{
				var cx = CreateFileContext(
					mode_.Options.FlattenDirectories && !mode_.IsWritable);

				files = dir_.GetFiles(cx);
			}

			return files;
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
			window_.ContentPanel.Layout = new VUI.BorderLayout();

			window_.ContentPanel.Add(CreateTop(), VUI.BorderLayout.Top);
			window_.ContentPanel.Add(CreateCenter(), VUI.BorderLayout.Center);
			window_.ContentPanel.Add(CreateBottom(), VUI.BorderLayout.Bottom);

			root_.ContentPanel.Add(window_, VUI.BorderLayout.Center);

			tree_.Init();
		}

		private VUI.Panel CreateTop()
		{
			optionsPanel_ = new OptionsPanel(this);
			addressBar_ = new AddressBar(this);

			var p = new VUI.Panel(new VUI.VerticalFlow(10));
			p.BackgroundColor = VUI.Style.Theme.SplitterHandleBackgroundColor;
			p.Padding = new VUI.Insets(10, 10, 10, 10);

			p.Add(addressBar_);
			p.Add(optionsPanel_);

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

			// needs work
			//var s = p.Add(new SearchBox("Search folders"), VUI.BorderLayout.Bottom);
			//s.TextBox.Borders = new VUI.Insets(0, 1, 1, 0);
			//s.Changed += (ss) => tree_.TreeView.Filter = ss;

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
			FS.Instrumentation.Start(FS.I.FDUpdateButton);
			{
				buttonsPanel_.SetActionButton(mode_.CanExecute(this));
			}
			FS.Instrumentation.End();
		}

		private void SetPath()
		{
			FS.Instrumentation.Start(FS.I.FDSetPath);
			{
				addressBar_.Path = dir_?.VirtualPath ?? "";
			}
			FS.Instrumentation.End();
		}

		private void SelectFromFilename()
		{
			if (SelectedFile == null || SelectedFile.Name != buttonsPanel_.Filename)
			{
				foreach (var f in files_)
				{
					if (f.Name == buttonsPanel_.Filename)
					{
						SelectFile(f);
						break;
					}
				}

				// filename not found, make selected file null, but don't
				// replace the filename, it'd be set to an empty string and
				// would be lost
				SelectFile(null, false);
			}
		}

		private void OnTreeSelection(IFileTreeItem item)
		{
			var c = (item as FileTreeItem)?.GetFSObject();
			if (c == null)
			{
				Log.Error($"selected a file tree item with a null object, path={(item as FileTreeItem)?.Path}");
				return;
			}

			SetCurrentDirectory(c);
			addressBar_.SetDirectory(c);
		}
	}
}
