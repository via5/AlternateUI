﻿using MVR.FileManagementSecure;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AUI.FileDialog
{
	class FileContextMenu : VUI.ContextMenu
	{
		private readonly FileDialog fd_;
		private FS.IFilesystemObject o_ = null;

		public FileContextMenu(FileDialog fd)
		{
			fd_ = fd;
		}

		public void Set(FS.IFilesystemObject o)
		{
			o_ = o;
		}
	}


	class FileDialog
	{
		public const int FontSize = 24;

		public const int SelectFileNoFlags = 0x00;
		public const int SelectFileKeepFilename = 0x01;
		public const int SelectFileScrollTo = 0x02;

		public const int SelectDirectoryNoFlags = 0x00;
		public const int SelectDirectoryExpand = 0x01;
		public const int SelectDirectoryDebug = 0x02;
		public const int SelectDirectoryWritableOnly = 0x04;


		private readonly Logger log_;

		private VUI.Root root_ = null;
		private VUI.Window window_ = null;

		private OptionsPanel optionsPanel_ = null;
		private AddressBar addressBar_ = null;
		private FileTree tree_ = null;
		private FilesPanel filesPanel_ = null;
		private ButtonsPanel buttonsPanel_ = null;
		private VUI.SearchBox packagesSearch_ = null;
		private FileContextMenu fileMenu_ = null;

		private IFileDialogMode mode_ = new NoMode();
		private FS.IFilesystemContainer dir_ = null;
		private List<FS.IFilesystemObject> files_ = null;
		private FS.IFilesystemObject selected_ = null;
		private Action<string> callback_ = null;
		private bool ignoreHistory_ = false;
		private bool ignoreDirSelection_ = false;


		public FileDialog()
		{
			log_ = new Logger("fd");
			//FS.Instrumentation.Instance.Enabled = true;
		}


		public void Enable()
		{
			AlternateUI.Instance.Sys
				.RegisterRefreshHandler(OnPackagesRefreshed);
		}

		public void Disable()
		{
			if (root_ != null)
				root_.Visible = false;

			AlternateUI.Instance.Sys
				.UnregisterRefreshHandler(OnPackagesRefreshed);
		}

		private void OnPackagesRefreshed()
		{
			try
			{
				if (root_ != null)
					Refresh();
			}
			catch (Exception e)
			{
				Log.Error("exception in OnPackagesRefreshed:");
				Log.Error(e.ToString());
			}
		}

		public bool Visible
		{
			get { return root_?.Visible ?? false; }
		}

		public bool UsePackageTime
		{
			get
			{
				return FS.Filesystem.Instance.UsePackageTime;
			}

			set
			{
				if (FS.Filesystem.Instance.UsePackageTime != value)
				{
					FS.Filesystem.Instance.UsePackageTime = value;
					Refresh();
				}
			}
		}

		public void Update(float s)
		{
			root_?.Update();

			if (Input.GetKey(KeyCode.Escape))
				Hide();

			if (FS.Instrumentation.Instance.Enabled)
				DumpInstrumentation(s);
		}

		private void DumpInstrumentation(float s)
		{
			FS.Instrumentation.Instance.UpdateTickers(s);

			if (FS.Instrumentation.Instance.Updated)
			{
				FS.Instrumentation.Instance.Dump(Log);
				FS.Instrumentation.Reset();
			}
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

		public VUI.Root Root
		{
			get { return root_; }
		}


		public FS.IFilesystemContainer SelectedDirectory
		{
			get { return dir_; }
		}

		public FS.IFilesystemContainer SelectedDirectoryRootInPinned
		{
			get { return tree_.SelectedRootInPinned; }
		}

		public FS.IFilesystemObject SelectedFile
		{
			get { return selected_; }
		}

		public string Filename
		{
			get { return buttonsPanel_.Filename; }
		}

		public string Search
		{
			get { return addressBar_.Search; }
		}

		public History History
		{
			get { return mode_.Options.History; }
		}


		public void Show(
			IFileDialogMode mode, Action<string> callback,
			string initialDir = null, string initialFile = null)
		{
			bool refreshDirs = (mode_ != mode);
			mode_ = mode;

			if (root_ == null)
				CreateUI();

			callback_ = callback;

			root_.Visible = true;
			window_.Title = mode_.Title;

			optionsPanel_.SetMode(mode_);
			buttonsPanel_.Set(mode_);

			tree_.Enable();

			if (refreshDirs)
			{
				try
				{
					ignoreDirSelection_ = true;
					dir_ = null;
					RefreshDirectories(false);
				}
				finally
				{
					ignoreDirSelection_ = false;
				}
			}
			else
			{
				FS.Instrumentation.Start(FS.I.FDTreeSetFlags);
				{
					tree_.SetFlags(GetTreeFlags());
				}
				FS.Instrumentation.End();
			}

			if (dir_ == null || dir_?.VirtualPath != initialDir)
				SelectInitialDirectory(initialDir);

			if (dir_ == null)
			{
				// SelectInitialDirectory() won't fire the selected event if the
				// node was already selected, so call it manually to make sure
				SetCurrentDirectory(tree_.Selected as FS.IFilesystemContainer, false);
			}

			if (!SelectInitialFile(mode_.Options.CurrentFile, initialFile))
				filesPanel_.ScrollToTop();

			addressBar_.Search = mode_.Options.Search;

			buttonsPanel_.FocusFilename();
		}

		public void Hide()
		{
			if (Visible)
			{
				tree_.Disable();
				root_.Visible = false;
			}
		}

		public bool CanGotoRVPDir(FS.IFilesystemObject o)
		{
			if (o == null)
				return false;

			FS.IFilesystemContainer dir;

			if (o.IsFile)
				dir = o.Parent;
			else
				dir = o as FS.IFilesystemContainer;

			if (dir != null)
			{
				var cx = CreateTreeContext(false);
				if (cx.MergePackages)
					return (dir.VirtualPath != dir.RelativeVirtualPath);
			}

			return false;
		}

		public void GotoRVPDir(FS.IFilesystemObject o)
		{
			if (o == null)
				return;

			FS.IFilesystemContainer dir;

			if (o.IsFile)
				dir = o.Parent;
			else
				dir = o as FS.IFilesystemContainer;

			if (dir != null)
			{
				if (SelectDirectory(dir.RelativeVirtualPath))
				{
					if (o.IsFile)
						SelectFile(o);
				}
			}
		}

		public void SelectFile(FS.IFilesystemObject o, int flags = SelectFileNoFlags)
		{
			Log.Info($"SelectFile {o}");

			if (selected_ != o)
			{
				if (selected_ != null)
					filesPanel_.SetSelected(selected_, false, false);

				selected_ = o;

				if (selected_ != null)
					filesPanel_.SetSelected(selected_, true, Bits.IsSet(flags, SelectFileScrollTo));
			}

			if (!Bits.IsSet(flags, SelectFileKeepFilename))
				UpdateFilename();

			UpdateActionButton();
		}

		public bool SelectDirectory(
			string vpath, int flags = SelectDirectoryNoFlags,
			int scrollTo = VUI.TreeView.ScrollToNearest)
		{
			Log.Info($"SelectDirectory '{vpath}'");

			if (Bits.IsSet(flags, SelectDirectoryWritableOnly))
			{
				var o = FS.Filesystem.Instance.Resolve(
					CreateFileContext(false), vpath,
					FS.Filesystem.ResolveDirsOnly);

				if (o == null)
				{
					Log.Error($"SelectDirectory: needs writable dir, but can't resolve '{vpath}'");
					return false;
				}
				else
				{
					if (!o.IsWritable)
					{
						Log.Error($"SelectDirectory: needs writable dir, but is not: '{vpath}'");
						return false;
					}
				}
			}

			bool b;
			FS.Instrumentation.Start(FS.I.FDTreeSelect);
			{
				b = tree_.Select(vpath, flags, scrollTo);
			}
			FS.Instrumentation.End();

			return b;
		}

		public bool SelectDirectoryInPinned(
			string vpath, string pinnedRoot, int flags = SelectDirectoryNoFlags,
			int scrollTo = VUI.TreeView.ScrollToNearest)
		{
			Log.Info($"SelectDirectoryInPinned '{vpath}', pinnedRoot='{pinnedRoot}'");

			if (Bits.IsSet(flags, SelectDirectoryWritableOnly))
			{
				var o = FS.Filesystem.Instance.Resolve(
					CreateFileContext(false), vpath,
					FS.Filesystem.ResolveDirsOnly);

				if (o == null)
				{
					Log.Error($"SelectDirectoryInPinned: needs writable dir, but can't resolve '{vpath}'");
					return false;
				}
				else
				{
					if (!o.IsWritable)
					{
						Log.Error($"SelectDirectoryInPinned: needs writable dir, but is not: {o}");
						return false;
					}
				}
			}

			bool b;
			FS.Instrumentation.Start(FS.I.FDTreeSelect);
			{
				b = tree_.SelectInPinned(vpath, pinnedRoot, flags, scrollTo);
			}
			FS.Instrumentation.End();

			return b;
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
				SelectDirectory(History.Back());
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
				SelectDirectory(History.Next());
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
					SelectDirectory(History.Current);
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

		public VUI.ContextMenu GetContextMenu(FilePanel p)
		{
			if (fileMenu_ == null)
				fileMenu_ = new FileContextMenu(this);

			fileMenu_.Set(p.Object);

			return fileMenu_;
		}

		public void OpenInExplorer(FS.IFilesystemObject o)
		{
			if (o != null)
			{
				if (o.IsFile)
					o = o.Parent;

				var rp = o.DeVirtualize();
				if (rp != "")
					SuperController.singleton.OpenFolderInExplorer(rp);
			}
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

		public void Cancel()
		{
			callback_?.Invoke("");
			callback_ = null;
			Hide();
		}


		public void Refresh()
		{
			Log.Info("refresh");

			FS.Filesystem.Instance.ClearCaches();
			Icons.ClearCache();

			// give some time for the panels to clear so there's a visual
			// feedback that a refresh has occurred
			filesPanel_.Clear();
			AlternateUI.Instance.StartCoroutine(CoRefresh());
		}

		private IEnumerator CoRefresh()
		{
			yield return new WaitForEndOfFrame();
			RefreshBoth();
		}

		public void RefreshBoth()
		{
			Log.Info("RefreshBoth");

			RefreshDirectories(false);
			RefreshFiles();
		}

		public void RefreshDirectories(bool select = true)
		{
			Log.Info($"RefreshDirectories select={select}");

			FS.Instrumentation.Start(FS.I.FDTreeSetFlags);
			{
				tree_.SetFlags(GetTreeFlags());
			}
			FS.Instrumentation.End();

			FS.Instrumentation.Start(FS.I.FDTreeRefresh);
			{
				Log.Info("refreshing tree");
				tree_.Refresh();
			}
			FS.Instrumentation.End();

			if (select)
			{
				var s = tree_.Selected as FS.IFilesystemContainer;
				if (s != null)
					SetCurrentDirectory(s, false);
			}
		}

		public void RefreshFiles()
		{
			Log.Info($"RefreshFiles");

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

			optionsPanel_.SetFiles(files_);


			bool filesRefreshed = false;

			if (selected_ != null)
			{
				foreach (var f in files_)
				{
					if (f.IsSameObject(selected_))
					{
						selected_ = null;
						SelectFile(f, SelectFileScrollTo);
						filesRefreshed = true;
						break;
					}
				}

				if (!filesRefreshed)
				{
					Log.Info($"selected file {selected_} is gone");
					SelectFile(null);
				}
			}

			if (!filesRefreshed)
				filesPanel_.ScrollToTop();

			UpdateActionButton();
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

		public FS.Context CreateFileContext(bool recursive)
		{
			int flags = MakeContextFlags(recursive, mode_.Options);

			if (dir_?.VirtualPath == FS.Filesystem.Instance.GetRoot().AllFlat.VirtualPath ||
				dir_?.VirtualPath == FS.Filesystem.Instance.GetRoot().VirtualPath)
			{
				// hack: the root and AllFlatDirectory objects already includes
				// the packages root in its directory list and so the merge flag
				// is not necessary
				//
				// in fact, it's _really_ slow because it iterates and resolves
				// every package for every directory, so the flag _must_ be
				// removed
				flags = flags & ~FS.Context.MergePackagesFlag;
			}

			string removePrefix = "";
			if (!Bits.IsSet(flags, FS.Context.ShowHiddenFilesFlag))
				removePrefix = mode_.RemovePrefix;

			return new FS.Context(
				addressBar_.Search,
				buttonsPanel_.SelectedExtension?.Extensions,
				mode_.PackageRoot,
				mode_.Options.Sort, mode_.Options.SortDirection,
				flags, "", removePrefix, mode_.Whitelist);
		}

		public FS.Context CreateTreeContext(bool recursive)
		{
			return new FS.Context(
				"", buttonsPanel_.SelectedExtension?.Extensions,
				mode_.PackageRoot,
				FS.Context.SortFilename, FS.Context.SortAscending,
				MakeContextFlags(recursive, mode_.Options),
				packagesSearch_.Text, "", mode_.Whitelist);
		}


		private void SetCurrentDirectory(FS.IFilesystemContainer o, bool scroll = true)
		{
			if (dir_ == o)
				return;

			Log.Info($"SetCurrentDirectory {o}");

			if (!ignoreHistory_)
				History.Push(o.VirtualPath);

			dir_ = o;
			addressBar_.SetDirectory(dir_);
			RefreshFiles();

			if (scroll)
				filesPanel_.ScrollToTop();
		}

		private void SelectInitialDirectory(string cwd)
		{
			var scrollTo = VUI.TreeView.ScrollToCenter;
			var flags = SelectDirectoryNoFlags;
			var opts = mode_.Options;

			if (mode_.IsWritable)
				flags |= SelectDirectoryWritableOnly;

			//Log.Info($"SelectInitialDirectory '{cwd}' '{opts.CurrentDirectory}' '{mode_.DefaultDirectory}' '{mode_.PackageRoot}'");

			if (!string.IsNullOrEmpty(cwd))
			{
				Log.Info($"trying cwd initial directory '{cwd}'");

				if (SelectDirectory(cwd, flags, scrollTo))
				{
					opts.CurrentDirectory = cwd;
					return;
				}

				Log.Error($"bad initial directory (cwd) {cwd}");
			}

			if (!string.IsNullOrEmpty(opts.CurrentFile))
			{
				var dir = FS.Path.Parent(opts.CurrentFile);

				if (!string.IsNullOrEmpty(opts.CurrentDirectoryInPinned))
				{
					Log.Info($"trying current file parent in pinned initial directory '{dir}' '{opts.CurrentDirectoryInPinned}'");

					if (SelectDirectoryInPinned(
							dir, opts.CurrentDirectoryInPinned,
							flags, scrollTo))
					{
						return;
					}
				}


				Log.Info($"trying current file parent initial directory '{dir}'");

				if (SelectDirectory(dir, flags, scrollTo))
					return;

				Log.Error($"bad initial directory for current file parent '{dir}'");
			}

			if (!string.IsNullOrEmpty(opts.CurrentDirectory))
			{
				if (!string.IsNullOrEmpty(opts.CurrentDirectoryInPinned))
				{
					Log.Info($"trying dir in pinned initial directory '{opts.CurrentDirectory}'");

					if (SelectDirectoryInPinned(
							opts.CurrentDirectory, opts.CurrentDirectoryInPinned,
							flags, scrollTo))
					{
						return;
					}

					Log.Error($"bad initial directory (opts current in pinned) '{opts.CurrentDirectory}' '{opts.CurrentDirectoryInPinned}'");
				}

				Log.Info($"trying current dir initial directory '{opts.CurrentDirectory}'");

				if (SelectDirectory(opts.CurrentDirectory, flags, scrollTo))
					return;

				Log.Error($"bad initial directory (opts current dir) {opts.CurrentDirectory}");
			}

			if (!string.IsNullOrEmpty(mode_.DefaultDirectory))
			{
				Log.Info($"trying default dir initial directory '{mode_.DefaultDirectory}'");

				if (SelectDirectory(mode_.DefaultDirectory, flags, scrollTo))
					return;

				Log.Error($"bad initial directory (mode default dir) {mode_.DefaultDirectory}");
			}

			Log.Error($"can't select any initial directory");
		}

		private bool SelectInitialFile(string path, string initialPath = null)
		{
			if (dir_ == null)
				return false;

			FS.StringView name;

			if (initialPath != null)
				name = FS.Path.Filename(initialPath);
			else if (path != null)
				name = FS.Path.Filename(path);
			else
				return false;

			if (name.Length > 0)
			{
				foreach (var f in GetFiles())
				{
					if (f.Name == name)
					{
						SelectFile(f, SelectFileScrollTo);
						return true;
					}
				}

				Log.Error($"bad initial file '{name}'");
			}

			buttonsPanel_.Filename = mode_.MakeNewFilename(this);
			return false;
		}

		private void UpdateFilename()
		{
			if (selected_ != null)
				buttonsPanel_.Filename = selected_.Name;
		}

		private IEnumerator CoRunCallback(Action<string> f, string path)
		{
			yield return new WaitForEndOfFrame();
			yield return new WaitForEndOfFrame();
			f?.Invoke(path);
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

			if (opts.LatestPackagesOnly)
				f |= FS.Context.LatestPackagesOnlyFlag;

			return f;
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
					if (po != null && po.IsSameObject(FS.Filesystem.Instance.GetPackagesRoot()))
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
			root_ = new VUI.Root(new VUI.TransformUIRootSupport(
				SuperController.singleton.fileBrowserUI.transform.parent),
				"filedialog");

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

			packagesSearch_ = p.Add(new VUI.SearchBox("Search packages"), VUI.BorderLayout.Bottom);
			packagesSearch_.TextBox.Borders = new VUI.Insets(0, 1, 1, 0);
			packagesSearch_.AutoComplete.Enabled = true;
			packagesSearch_.AutoComplete.Height = 400;
			packagesSearch_.AutoComplete.File = AlternateUI.Instance.GetConfigFilePath(
				"aui.filedialog.packagesSearch.autocomplete.json");

			packagesSearch_.Changed += (ss) =>
			{
				if (string.IsNullOrEmpty(ss))
					packagesSearch_.TextBox.BackgroundColor = new Color(0, 0, 0, 0);
				else
					packagesSearch_.TextBox.BackgroundColor = new Color(0, 0.2f, 0);

				tree_.SearchPackages(ss);
			};

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
				SelectFile(null);
			}
		}

		private void OnTreeSelection(IFileTreeItem item)
		{
			if (ignoreDirSelection_) return;

			Log.Info($"OnTreeSelection {item}");

			var c = (item as FileTreeItem)?.GetFSObject();
			if (c == null)
			{
				Log.Error($"selected a file tree item with a null object, path={(item as FileTreeItem)?.Path}");
				return;
			}

			SetCurrentDirectory(c);
		}
	}
}
