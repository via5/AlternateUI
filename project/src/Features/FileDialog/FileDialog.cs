using MVR.FileManagementSecure;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AUI.FileDialog
{
	public static class Icons
	{
		private static Texture packageIcon_ = null;
		private static readonly List<Action<Texture>> packageIconCallbacks_ =
			new List<Action<Texture>>();

		public static void LoadAll()
		{
			ImageLoaderThreaded.QueuedImage queuedImage = new ImageLoaderThreaded.QueuedImage();
			queuedImage.imgPath = AlternateUI.Instance.PluginPath + "/res/package.png";
			queuedImage.linear = true;

			queuedImage.callback = (tt) =>
			{
				packageIcon_ = tt.tex;

				foreach (var f in packageIconCallbacks_)
					f(packageIcon_);

				packageIconCallbacks_.Clear();
			};

			ImageLoaderThreaded.singleton.ClearCacheThumbnail(queuedImage.imgPath);
			ImageLoaderThreaded.singleton.QueueThumbnail(queuedImage);
		}

		public static void GetPackageIcon(Action<Texture> f)
		{
			if (packageIcon_ != null)
			{
				f(packageIcon_);
			}
			else
			{
				packageIconCallbacks_.Add(f);
			}
		}

		public static void GetDirectoryIcon(Action<Texture> f)
		{
			f(SuperController.singleton.fileBrowserUI.folderIcon.texture);
		}
	}


	class FileDialog : BasicFeature
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

		private const int FontSize = 24;

		private VUI.Root root_ = null;
		private readonly Dictionary<string, File[]> cachedFiles_ = new Dictionary<string, File[]>();
		private readonly Dictionary<string, File[]> cachedDirs_ = new Dictionary<string, File[]>();
		private File[] cachedPackagesFlattened_ = null;
		private File[] files_ = new File[0];
		private FileTree tree_ = null;
		private FilePanel[] panels_ = new FilePanel[0];
		private VUI.ScrollBar sb_ = null;
		private string cwd_;
		private int top_ = 0;

		public FileDialog()
			: base("fileDialog", "File dialog", false)
		{
			cwd_ = null;
		}

		public int Columns
		{
			get { return 5; }
		}

		public int Rows
		{
			get { return 5; }
		}

		public string CurrentDirectory
		{
			get { return cwd_; }
		}

		public void SetCurrentDirectory(string s)
		{
			Log.Info($"cwd={s}");

			cwd_ = s;
			top_ = 0;
			GetFiles();
			SetPanels(0);
		}

		public void SetPackagesFlattened()
		{
			Log.Info($"packages flattened");

			cwd_ = null;
			top_ = 0;
			GetPackagesFlattened();
			SetPanels(0);
		}

		public void SetAllFlattened()
		{
			Log.Info($"all flattened");

			cwd_ = null;
			top_ = 0;
			GetAllFlattened();
			SetPanels(0);
		}

		protected override void DoEnable()
		{
			if (root_ == null)
			{
				Icons.LoadAll();

				root_ = new VUI.Root(new VUI.TransformUIRootSupport(
					SuperController.singleton.fileBrowserUI.transform.parent));

				root_.ContentPanel.Layout = new VUI.BorderLayout();

				Create();
				GetFiles();

				tree_.Update("Saves/scene");
				SetPanels(0);
			}
		}

		private void Create()
		{
			CreateTree();
			CreateFilesPanel();
		}

		private void CreateTree()
		{
			tree_ = new FileTree(FontSize);

			tree_.PathSelected += OnPathSelected;
			tree_.PackageSelected += OnPackageSelected;
			tree_.AllFlattened += OnAllFlattened;
			tree_.PackagesFlattened += OnPackagesFlattened;

			root_.ContentPanel.Add(tree_.Widget, VUI.BorderLayout.Left);
		}

		private void CreateFilesPanel()
		{
			var gl = new VUI.GridLayout(Columns);
			gl.UniformWidth = true;
			var files = new VUI.Panel(gl);

			panels_ = new FilePanel[Columns * Rows];

			for (int j = 0; j < Columns * Rows; ++j)
			{
				panels_[j] = new FilePanel(FontSize);
				files.Add(panels_[j].Panel);
			}

			sb_ = new VUI.ScrollBar();
			sb_.MinimumSize = new VUI.Size(VUI.Style.Metrics.ScrollBarWidth, 0);

			sb_.ValueChanged += (v) =>
			{
				int y = Math.Max(0, (int)(v / root_.Bounds.Height));

				if (top_ != y)
				{
					top_ = y;
					Log.Info($"{top_}");
					SetPanels(top_ * Columns);
				}
			};

			var filesPanel = new VUI.Panel(new VUI.BorderLayout());
			filesPanel.Add(files, VUI.BorderLayout.Center);
			filesPanel.Add(sb_, VUI.BorderLayout.Right);

			root_.ContentPanel.Add(filesPanel, VUI.BorderLayout.Center);
		}

		private void GetFiles()
		{
			if (string.IsNullOrEmpty(CurrentDirectory))
			{
				SetFiles(null);
				return;
			}

			var list = new List<File>();
			GetFilesRecursive(CurrentDirectory, list);
			SetFiles(list);
		}

		private void SetFiles(List<File> list)
		{
			if (list == null)
			{
				files_ = new File[0];
			}
			else
			{
				U.NatSort(list);
				files_ = list.ToArray();
			}

			if (root_.Bounds.Height <= 0)
				sb_.Range = 0;
			else
				sb_.Range = files_.Length / Columns * root_.Bounds.Height;
		}

		private void GetPackagesFlattened()
		{
			CachePackagesFlattened();

			var list = new List<File>(cachedPackagesFlattened_);
			SetFiles(list);
		}

		private void GetAllFlattened()
		{
			CachePackagesFlattened();

			var list = new List<File>(cachedPackagesFlattened_);
			GetFilesRecursive("Saves/scene", list);

			SetFiles(list);
		}

		private void CachePackagesFlattened()
		{
			if (cachedPackagesFlattened_ != null)
				return;

			var list = new List<File>();

			foreach (var p in FileManagerSecure.GetShortCutsForDirectory("Saves/scene"))
			{
				if (string.IsNullOrEmpty(p.package))
					continue;

				if (!string.IsNullOrEmpty(p.packageFilter))
					continue;

				GetFilesRecursive(p.path, list);
			}

			cachedPackagesFlattened_ = list.ToArray();
		}

		private void GetFilesRecursive(string parent, List<File> list)
		{
			list.AddRange(GetFiles(parent));

			foreach (var d in GetDirectories(parent))
				GetFilesRecursive(d.Path, list);
		}

		private File[] GetFiles(string parent)
		{
			File[] fs;

			if (!cachedFiles_.TryGetValue(parent, out fs))
			{
				var exts = new string[] { ".json", ".vac", ".vap", ".vam", ".scene" };
				var list = new List<File>();

				foreach (var f in FileManagerSecure.GetFiles(parent))
				{
					foreach (var e in exts)
					{
						if (f.EndsWith(e))
						{
							list.Add(new File(f));
							break;
						}
					}
				}

				fs = list.ToArray();
				cachedFiles_.Add(parent, fs);
			}

			return fs;
		}

		private File[] GetDirectories(string parent)
		{
			File[] fs;
			if (cachedDirs_.TryGetValue(parent, out fs))
				return fs;

			var list = new List<File>();
			foreach (var d in FileManagerSecure.GetDirectories(parent))
				list.Add(new File(d));

			fs = list.ToArray();
			cachedDirs_.Add(parent, fs);

			return fs;
		}

		private void SetPanels(int from)
		{
			int panelIndex = 0;
			for (int i=from; i<files_.Length; ++i)
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

		protected override void DoUpdate(float s)
		{
			root_?.Update();
		}

		private void OnPathSelected(string p)
		{
			SetCurrentDirectory(p);
		}

		private void OnPackageSelected(string name)
		{
			SetCurrentDirectory(name);
		}

		private void OnAllFlattened()
		{
			SetAllFlattened();
		}

		private void OnPackagesFlattened()
		{
			SetPackagesFlattened();
		}

		public override string Description
		{
			get { return "Replaces the file dialogs."; }
		}
	}
}
