using MVR.FileManagementSecure;
using SimpleJSON;
using System;
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


	static class Cache
	{
		private static readonly Dictionary<string, List<File>> files_ =
			new Dictionary<string, List<File>>();

		private static readonly Dictionary<string, List<File>> dirs_ =
			new Dictionary<string, List<File>>();

		private static List<File> packagesFlat_ = null;


		public static List<File> GetDirectories(string parent)
		{
			List<File> list = null;
			dirs_.TryGetValue(parent, out list);
			return list;
		}

		public static void AddDirectories(string parent, List<File> list)
		{
			dirs_.Add(parent, list);
		}

		public static List<File> GetFiles(string parent)
		{
			List<File> list = null;
			files_.TryGetValue(parent, out list);
			return list;
		}

		public static void AddFiles(string parent, List<File> list)
		{
			files_.Add(parent, list);
		}

		public static List<File> GetPackagesFlat()
		{
			return packagesFlat_;
		}

		public static void SetPackagesFlat(List<File> list)
		{
			packagesFlat_ = list;
		}
	}


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


	interface IFileContainer
	{
		List<File> GetFiles(FileDialog fd);
	}


	class EmptyContainer : IFileContainer
	{
		public List<File> GetFiles(FileDialog fd)
		{
			return new List<File>();
		}
	}


	abstract class FSContainer : IFileContainer
	{
		public abstract List<File> GetFiles(FileDialog fd);

		protected void GetFilesRecursive(string parent, List<File> list)
		{
			list.AddRange(GetFiles(parent));

			foreach (var d in GetDirectories(parent))
				GetFilesRecursive(d.Path, list);
		}

		protected List<File> GetDirectories(string parent)
		{
			List<File> fs = Cache.GetDirectories(parent);

			if (fs == null)
			{
				fs = new List<File>();
				foreach (var d in FileManagerSecure.GetDirectories(parent))
					fs.Add(new File(d));

				Cache.AddDirectories(parent, fs);
			}

			return fs;
		}

		protected List<File> GetFiles(string parent)
		{
			List<File> fs = Cache.GetFiles(parent);

			if (fs == null)
			{
				var exts = new string[] { ".json", ".vac", ".vap", ".vam", ".scene" };
				fs = new List<File>();

				foreach (var f in FileManagerSecure.GetFiles(parent))
				{
					foreach (var e in exts)
					{
						if (f.EndsWith(e))
						{
							fs.Add(new File(f));
							break;
						}
					}
				}

				Cache.AddFiles(parent, fs);
			}

			return fs;
		}

		protected List<File> GetPackagesFlat()
		{
			var list = new List<File>();

			foreach (var p in FileManagerSecure.GetShortCutsForDirectory("Saves/scene"))
			{
				if (string.IsNullOrEmpty(p.package))
					continue;

				if (!string.IsNullOrEmpty(p.packageFilter))
					continue;

				GetFilesRecursive(p.path, list);
			}

			return list;
		}
	}


	class DirectoryContainer : FSContainer
	{
		private readonly string path_;

		public DirectoryContainer(string path)
		{
			path_ = path;
		}

		public override List<File> GetFiles(FileDialog fd)
		{
			return GetFiles(fd.FlattenDirectories);
		}

		protected List<File> GetFiles(bool flatten)
		{
			if (string.IsNullOrEmpty(path_))
				return new List<File>();

			List<File> list;

			if (flatten)
			{
				list = new List<File>();
				GetFilesRecursive(path_, list);
			}
			else
			{
				list = GetFiles(path_);
			}

			return list;
		}
	}


	class PackageContainer : DirectoryContainer
	{
		private readonly ShortCut sc_;

		public PackageContainer(ShortCut sc)
			: base(sc.path)
		{
			sc_ = sc;
		}

		public override List<File> GetFiles(FileDialog fd)
		{
			return GetFiles(fd.FlattenPackages);
		}
	}


	class PackagesFlatContainer : FSContainer
	{
		public override List<File> GetFiles(FileDialog fd)
		{
			return GetPackagesFlat();
		}
	}


	class AllFlatContainer : FSContainer
	{
		public override List<File> GetFiles(FileDialog fd)
		{
			var list = new List<File>();

			{
				var packages = Cache.GetPackagesFlat();

				if (packages == null)
				{
					packages = GetPackagesFlat();
					Cache.SetPackagesFlat(packages);
				}

				list.AddRange(packages);
			}

			GetFilesRecursive("Saves/scene", list);

			return list;
		}

	}


	class FileDialog : BasicFeature
	{
		private const int FontSize = 24;

		private VUI.Root root_ = null;
		private IFileContainer container_ = new EmptyContainer();
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
			get { return 5; }
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

				tree_.Rebuild();
				UpdateFlatten();

				SetContainer(new EmptyContainer());
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
			tree_.AllFlatSelected += OnAllFlat;
			tree_.PackagesFlatSelected += OnPackagesFlat;
			tree_.NothingSelected += OnNothingSelected;

			var top = new VUI.Panel(new VUI.HorizontalFlow());
			top.Add(new VUI.CheckBox("Flatten subfolders", b => FlattenDirectories = b, flattenDirs_));
			top.Add(new VUI.CheckBox("Flatten packages", b => FlattenPackages = b, flattenPackages_));

			var p = new VUI.Panel(new VUI.BorderLayout());
			p.Add(tree_.Widget, VUI.BorderLayout.Center);
			p.Add(top, VUI.BorderLayout.Top);

			root_.ContentPanel.Add(p, VUI.BorderLayout.Left);
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

		private void SetFiles(List<File> list)
		{
			U.NatSort(list);
			files_ = list;

			if (root_.Bounds.Height <= 0)
				sb_.Range = 0;
			else
				sb_.Range = files_.Count / Columns * root_.Bounds.Height;
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
