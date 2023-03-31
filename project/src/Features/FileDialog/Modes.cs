using MVR.FileManagementSecure;
using SimpleJSON;
using System;
using System.Collections.Generic;

namespace AUI.FileDialog
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


	class History
	{
		private const int Max = 30;

		private readonly List<string> paths_ = new List<string>();
		private int index_ = -1;

		public string Current
		{
			get
			{
				if (index_ < 0 || index_ >= paths_.Count)
					return "";

				return paths_[index_];
			}
		}

		public int CurrentIndex
		{
			get { return index_; }
		}

		public string[] Entries
		{
			get { return paths_.ToArray(); }
		}

		public bool CanGoBack()
		{
			return (paths_.Count > 0 && index_ >= 1);
		}

		public bool CanGoNext()
		{
			return paths_.Count > 0 && ((index_ + 1) < paths_.Count);
		}

		public string Back()
		{
			if (CanGoBack())
				--index_;

			return Current;
		}

		public string Next()
		{
			if (CanGoNext())
				++index_;

			return Current;
		}

		public bool SetIndex(int i)
		{
			if (i < 0 || i >= paths_.Count)
				return false;

			index_ = i;
			return true;
		}

		public void Push(string s)
		{
			if (index_ >= 0 && (index_ + 1) < paths_.Count)
				paths_.RemoveRange(index_ + 1, paths_.Count - (index_ + 1));

			while (paths_.Count > Max)
				paths_.RemoveAt(0);

			if (paths_.Count > 0 && paths_[paths_.Count - 1] == s)
				return;

			paths_.Add(s);
			++index_;
		}
	}


	class Options
	{
		private string name_;
		private string lastFile_;
		private string lastDir_;
		private string lastDirInPinned_;
		private bool flattenDirs_;
		private bool flattenPackages_;
		private bool mergePackages_;
		private bool showHiddenFolders_;
		private bool showHiddenFiles_;
		private bool latestPackagesOnly_;
		private int sort_, sortDir_;
		private float scroll_ = 0;
		private string search_ = "";
		private readonly History history_ = new History();

		private static readonly Dictionary<string, Options> map_ =
			new Dictionary<string, Options>();

		public Options(
			string name,
			bool flattenDirs, bool flattenPackages, bool mergePackages,
			bool showHiddenFolders, bool showHiddenFiles, int sort, int sortDir)
		{
			name_ = name;
			lastFile_ = null;
			lastDirInPinned_ = null;
			flattenDirs_ = flattenDirs;
			flattenPackages_ = flattenPackages;
			mergePackages_ = mergePackages;
			showHiddenFolders_ = showHiddenFolders;
			showHiddenFiles_ = showHiddenFiles;
			latestPackagesOnly_ = true;
			sort_ = sort;
			sortDir_ = sortDir;
		}

		public static Options Get(string name)
		{
			if (string.IsNullOrEmpty(name))
				return null;

			Options o = null;
			map_.TryGetValue(name, out o);
			return o;
		}

		public static void Set(string name, Options o)
		{
			if (!string.IsNullOrEmpty(name))
				map_.Add(name, o);
		}

		public History History
		{
			get { return history_; }
		}

		public bool FlattenDirectories
		{
			get { return flattenDirs_; }
			set { flattenDirs_ = value; Changed(); }
		}

		public bool FlattenPackages
		{
			get { return flattenPackages_; }
			set { flattenPackages_ = value; Changed(); }
		}

		public bool MergePackages
		{
			get { return mergePackages_; }
			set { mergePackages_ = value; Changed(); }
		}

		public bool ShowHiddenFolders
		{
			get { return showHiddenFolders_; }
			set { showHiddenFolders_ = value; Changed(); }
		}

		public bool ShowHiddenFiles
		{
			get { return showHiddenFiles_; }
			set { showHiddenFiles_ = value; Changed(); }
		}

		public bool LatestPackagesOnly
		{
			get { return latestPackagesOnly_; }
			set { latestPackagesOnly_ = value; Changed(); }
		}

		public int Sort
		{
			get { return sort_; }
			set { sort_ = value; Changed(); }
		}

		public int SortDirection
		{
			get { return sortDir_; }
			set { sortDir_ = value; Changed(); }
		}

		public string Search
		{
			get { return search_; }
			set { search_ = value; }
		}

		public float Scroll
		{
			get { return scroll_; }
			set { scroll_ = value; }
		}

		public string CurrentDirectory
		{
			get
			{
				return lastDir_;
			}

			set
			{
				lastDir_ = value;
				Changed();
			}
		}

		public string CurrentDirectoryInPinned
		{
			get
			{
				return lastDirInPinned_;
			}

			set
			{
				lastDirInPinned_ = value;
				Changed();
			}
		}

		public string CurrentFile
		{
			get
			{
				return lastFile_;
			}

			set
			{
				lastFile_ = value;
				Changed();
			}
		}

		public void Load()
		{
			var path = GetOptionsFile();
			if (string.IsNullOrEmpty(path) || !FileManagerSecure.FileExists(path))
				return;

			var j = SuperController.singleton.LoadJSON(path)?.AsObject;
			if (j == null)
				return;

			if (j.HasKey("lastFile"))
				lastFile_ = j["lastFile"].Value;

			if (j.HasKey("lastDir"))
				lastDir_ = j["lastDir"].Value;

			if (j.HasKey("lastDirInPinned"))
				lastDirInPinned_ = j["lastDirInPinned"].Value;

			if (j.HasKey("flattenDirectories"))
				flattenDirs_ = j["flattenDirectories"].AsBool;

			if (j.HasKey("flattenPackages"))
				flattenPackages_ = j["flattenPackages"].AsBool;

			if (j.HasKey("mergePackages"))
				mergePackages_ = j["mergePackages"].AsBool;

			if (j.HasKey("showHiddenFolders"))
				showHiddenFolders_ = j["showHiddenFolders"].AsBool;

			if (j.HasKey("showHiddenFiles"))
				showHiddenFiles_ = j["showHiddenFiles"].AsBool;

			if (j.HasKey("latestPackagesOnly"))
				latestPackagesOnly_ = j["latestPackagesOnly"].AsBool;

			if (j.HasKey("sort"))
				sort_ = j["sort"].AsInt;

			if (j.HasKey("sortDirection"))
				sortDir_ = j["sortDirection"].AsInt;
		}

		public void Save()
		{
			var path = GetOptionsFile();
			if (string.IsNullOrEmpty(path))
				return;

			var j = new JSONClass();

			j["lastFile"] = new JSONData(lastFile_);
			j["lastDir"] = new JSONData(lastDir_);
			j["lastDirInPinned"] = new JSONData(lastDirInPinned_);
			j["flattenDirectories"] = new JSONData(flattenDirs_);
			j["flattenPackages"] = new JSONData(flattenPackages_);
			j["mergePackages"] = new JSONData(mergePackages_);
			j["showHiddenFolders"] = new JSONData(showHiddenFolders_);
			j["showHiddenFiles"] = new JSONData(showHiddenFiles_);
			j["latestPackagesOnly"] = new JSONData(latestPackagesOnly_);
			j["sort"] = new JSONData(sort_);
			j["sortDirection"] = new JSONData(sortDir_);

			SuperController.singleton.SaveJSON(j, GetOptionsFile());
		}

		private string GetOptionsFile()
		{
			if (string.IsNullOrEmpty(name_))
				return null;

			return AlternateUI.Instance.GetConfigFilePath(
				$"aui.filedialog.modes.{name_}.json");
		}

		protected void Changed()
		{
			Save();
		}
	}


	public delegate void ExecuteHandler();

	interface IFileDialogMode
	{
		Options Options { get; }

		string Title { get; set; }
		ExtensionItem[] Extensions { get; }
		string PackageRoot { get; }
		string DefaultDirectory { get; set; }
		string ActionText { get; }
		bool IsWritable { get; }
		FS.Whitelist Whitelist { get; }

		bool CanExecute(FileDialog fd);
		void Execute(FileDialog fd, ExecuteHandler h);
		string GetPath(FileDialog fd);
		string MakeNewFilename(FileDialog fd);
	}


	abstract class BasicMode : IFileDialogMode
	{
		private readonly string name_;
		private string title_;
		private readonly ExtensionItem[] exts_;
		private readonly string packageRoot_;
		private string defaultPath_;
		private readonly FS.Whitelist whitelist_;
		private readonly Options opts_;

		protected BasicMode(
			string name, string title, ExtensionItem[] exts,
			string packageRoot, string defaultPath,
			bool flattenDirs, bool flattenPackages, bool mergePackages,
			bool showHiddenFolders, bool showHiddenFiles, int sort, int sortDir,
			FS.Whitelist whitelist)
		{
			name_ = name;
			title_ = title;
			exts_ = exts;
			packageRoot_ = packageRoot;
			defaultPath_ = defaultPath;
			whitelist_ = whitelist;

			opts_ = Options.Get(name_);
			if (opts_ == null)
			{
				opts_ = new Options(
					name_, flattenDirs, flattenPackages,
					mergePackages, showHiddenFolders, showHiddenFiles,
					sort, sortDir);

				opts_.Load();

				Options.Set(name_, opts_);
			}
		}

		public Options Options
		{
			get { return opts_; }
		}

		public string Title
		{
			get { return title_; }
			set { title_ = value; }
		}

		public ExtensionItem[] Extensions
		{
			get { return exts_; }
		}

		public string PackageRoot
		{
			get { return packageRoot_; }
		}

		public string DefaultDirectory
		{
			get { return defaultPath_; }
			set { defaultPath_ = value; }
		}

		public FS.Whitelist Whitelist
		{
			get { return whitelist_; }
		}

		public abstract string ActionText { get; }
		public abstract bool IsWritable { get; }

		public abstract bool CanExecute(FileDialog fd);
		public abstract string GetPath(FileDialog fd);
		public abstract string MakeNewFilename(FileDialog fd);

		public void Execute(FileDialog fd, ExecuteHandler h)
		{
			opts_.CurrentFile = fd.SelectedFile?.VirtualPath;
			opts_.CurrentDirectory = fd.SelectedDirectory?.VirtualPath;
			opts_.CurrentDirectoryInPinned = fd.SelectedDirectoryRootInPinned?.VirtualPath ?? "";
			opts_.Search = fd.Search;
			opts_.Scroll = fd.Scroll;

			DoExecute(fd, h);
		}

		protected virtual void DoExecute(FileDialog fd, ExecuteHandler h)
		{
			h();
		}
	}


	class NoMode : BasicMode
	{
		public NoMode()
			: base(
				  null, "", new ExtensionItem[0], "", "",
				  false, false, false, false, false,
				  FS.Context.NoSort, FS.Context.NoSortDirection, null)
		{
		}

		public override string ActionText
		{
			get { return ""; }
		}

		public override bool IsWritable
		{
			get { return false; }
		}

		public override bool CanExecute(FileDialog fd)
		{
			return false;
		}

		public override string GetPath(FileDialog fd)
		{
			return "";
		}

		public override string MakeNewFilename(FileDialog fd)
		{
			return "";
		}
	}


	class OpenMode : BasicMode
	{
		public OpenMode(
			string name, string title, ExtensionItem[] exts,
			string packageRoot, string defaultPath,
			bool flattenDirs, bool flattenPackages, bool mergePackages,
			bool showHiddenFolders, bool showHiddenFiles, int sort, int sortDir,
			FS.Whitelist whitelist)
				: base(
					  name, title, exts, packageRoot, defaultPath,
					  flattenDirs, flattenPackages, mergePackages,
					  showHiddenFolders, showHiddenFiles, sort, sortDir,
					  whitelist)
		{
		}

		public override string ActionText
		{
			get { return "Open"; }
		}

		public override bool IsWritable
		{
			get { return false; }
		}

		public override bool CanExecute(FileDialog fd)
		{
			return (fd.SelectedFile != null || fd.Filename != "");
		}

		public override string GetPath(FileDialog fd)
		{
			string path = "";

			var s = fd.SelectedFile;
			if (s == null)
			{
				var cwd = fd.SelectedDirectory;

				if (cwd != null)
				{
					var dir = cwd.MakeRealPath().Trim();
					if (dir == "")
						return "";

					var file = fd.Filename?.Trim() ?? "";
					if (file == "")
						return "";

					path = FS.Path.Join(dir, file);
				}
			}
			else
			{
				path = s.MakeRealPath();
			}

			if (path == "")
				return "";

			return FileManagerSecure.GetFullPath(path);
		}

		public override string MakeNewFilename(FileDialog fd)
		{
			return "";
		}
	}


	class SaveMode : BasicMode
	{
		public SaveMode(
			string name, string title, ExtensionItem[] exts,
			string packageRoot, string defaultPath,
			bool flattenDirs, bool flattenPackages, bool mergePackages,
			bool showHiddenFolders, bool showHiddenFiles, int sort, int sortDir,
			FS.Whitelist whitelist)
				: base(
					  name, title, exts, packageRoot, defaultPath,
					  flattenDirs, flattenPackages, mergePackages,
					  showHiddenFolders, showHiddenFiles, sort, sortDir,
					  whitelist)
		{
		}

		public override string ActionText
		{
			get { return "Save"; }
		}

		public override bool IsWritable
		{
			get { return true; }
		}

		public override bool CanExecute(FileDialog fd)
		{
			return (GetPath(fd) != "");
		}

		protected override void DoExecute(FileDialog fd, ExecuteHandler h)
		{
			var path = GetPath(fd);

			// useless, can't force vam to overwrite, so the warning is
			// shown twice
			//
			//if (FileManagerSecure.FileExists(path))
			//{
			//	var d = new VUI.TaskDialog(
			//		fd.Root, "File already exists",
			//		"The file already exists. Replace?",
			//		path);
			//
			//	d.AddButton(VUI.Buttons.OK, "Replace");
			//	d.AddButton(VUI.Buttons.Cancel, "Cancel");
			//
			//	d.RunDialog((r) =>
			//	{
			//		if (r == VUI.Buttons.OK)
			//		{
			//			h();
			//		}
			//	});
			//}

			fd.SelectedDirectory?.ClearCache();
			fd.SelectedFile?.ClearCache();

			h();
		}

		public override string GetPath(FileDialog fd)
		{
			var cwd = fd.SelectedDirectory;
			if (cwd == null)
				return "";

			var dir = cwd.DeVirtualize()?.Trim() ?? "";
			if (dir == "")
				return "";

			var file = fd.Filename?.Trim() ?? "";
			if (file == "")
				return "";

			if (file.IndexOf('.') == -1)
				file += fd.GetDefaultExtension();

			return FS.Path.Join(dir, file);
		}

		public override string MakeNewFilename(FileDialog fd)
		{
			var now = DateTime.UtcNow - new DateTime(1970, 1, 1);
			return ((int)now.TotalSeconds).ToString() + fd.GetDefaultExtension();
		}
	}


	static class Modes
	{
		struct ModeInfo
		{
			public string type, loadCaption, saveCaption;
			public ExtensionItem[] extensions;

			public ModeInfo(
				string type, string loadCaption, string saveCaption,
				ExtensionItem[] extensions)
			{
				this.type = type;
				this.loadCaption = loadCaption;
				this.saveCaption = saveCaption;
				this.extensions = extensions;
			}
		}

		static IFileDialogMode openScene_ = null;
		static IFileDialogMode saveScene_ = null;
		static IFileDialogMode openCUA_ = null;
		static IFileDialogMode openPlugin_ = null;

		static readonly Dictionary<string, IFileDialogMode> openPreset_ =
			new Dictionary<string, IFileDialogMode>();

		static readonly Dictionary<string, IFileDialogMode> savePreset_ =
			new Dictionary<string, IFileDialogMode>();

		static readonly Dictionary<string, IFileDialogMode> openTexture_ =
			new Dictionary<string, IFileDialogMode>();


		public static IFileDialogMode OpenScene(string caption = null)
		{
			if (caption == null)
				caption = "Open scene";

			if (openScene_ == null)
			{
				openScene_ = new OpenMode(
					"scene", caption,
					FileDialogFeature.GetSceneExtensions(true),
					"Saves/scene", "VaM/Saves/scene",
					true, true, true, false, false,
					FS.Context.SortDateCreated, FS.Context.SortDescending,
					new FS.Whitelist(new string[] { "VaM/Saves/scene", "VaM/Saves/Downloads" }));
			}
			else
			{
				openScene_.Title = caption;
			}

			return openScene_;
		}

		public static IFileDialogMode SaveScene()
		{
			if (saveScene_ == null)
			{
				saveScene_ = new SaveMode(
					"scene", "Save scene",
					FileDialogFeature.GetSceneExtensions(false),
					"Saves/scene", "VaM/Saves/scene",
					false, false, false, false, false,
					FS.Context.SortDateCreated, FS.Context.SortDescending,
					new FS.Whitelist(new string[] { "VaM/Saves/scene", "VaM/Saves/Downloads" }));
			}

			return saveScene_;
		}

		public static IFileDialogMode OpenCUA()
		{
			if (openCUA_ == null)
			{
				openCUA_ = new OpenMode(
					"cua", "Open asset bundle",
					FileDialogFeature.GetCUAExtensions(true),
					"Custom/Assets", "VaM/Custom/Assets",
					true, true, true, false, false,
					FS.Context.SortDateCreated, FS.Context.SortDescending,
					new FS.Whitelist(new string[] { "VaM/Custom/Assets" }));
			}

			return openCUA_;
		}

		public static IFileDialogMode OpenPlugin()
		{
			if (openPlugin_ == null)
			{
				openPlugin_ = new OpenMode(
					"plugin", "Open plugin",
					FileDialogFeature.GetPluginExtensions(true),
					"Custom/Scripts", "VaM/Custom/Scripts",
					false, true, true, false, false,
					FS.Context.SortDateCreated, FS.Context.SortDescending,
					new FS.Whitelist(new string[] { "VaM/Custom/Scripts" }));
			}

			return openPlugin_;
		}

		public static IFileDialogMode OpenPreset(string path)
		{
			var info = GetPresetInfo(path);

			IFileDialogMode m;

			if (!openPreset_.TryGetValue(path, out m))
			{
				m = new OpenMode(
					info.type, info.loadCaption, info.extensions,
					MakePackageRoot(path), MakeDefaultPath(path),
					false, true, true, false, false,
					FS.Context.SortDateCreated, FS.Context.SortDescending,
					new FS.Whitelist(new string[] { MakeWhitelist(path) }));

				openPreset_.Add(path, m);
			}

			return m;
		}

		public static IFileDialogMode SavePreset(string path)
		{
			var info = GetPresetInfo(path);

			IFileDialogMode m;

			if (!savePreset_.TryGetValue(path, out m))
			{
				m = new SaveMode(
					info.type, info.saveCaption, info.extensions,
					MakePackageRoot(path), MakeDefaultPath(path),
					false, true, true, false, false,
					FS.Context.SortDateCreated, FS.Context.SortDescending,
					new FS.Whitelist(new string[] { MakeWhitelist(path) }));

				savePreset_.Add(path, m);
			}

			return m;
		}

		public static IFileDialogMode OpenTexture(string path)
		{
			var info = GetTextureInfo(path);

			IFileDialogMode m;

			if (!openTexture_.TryGetValue(path, out m))
			{
				m = new OpenMode(
					info.type, info.loadCaption, info.extensions,
					MakePackageRoot(path), MakeDefaultPath(path),
					false, true, true, false, false,
					FS.Context.SortDateCreated, FS.Context.SortDescending,
					new FS.Whitelist(new string[] { MakeWhitelist(path) }));

				openTexture_.Add(path, m);
			}

			return m;
		}


		private static string MakePackageRoot(string path)
		{
			int col = path.IndexOf(":");
			if (col == -1)
				return path;

			path = path.Substring(col + 1);
			if (path.StartsWith("/"))
				path = path.Substring(1);

			return path;
		}

		private static string MakeWhitelist(string path)
		{
			return "VaM/" + MakePackageRoot(path);
		}

		private static string MakeDefaultPath(string path)
		{
			return FS.Filesystem.Instance.MakeFSPath(path);
		}

		private static ModeInfo GetPresetInfo(string path)
		{
			path = path.Replace("\\", "/");

			switch (path)
			{
				case "Custom/Atom/Person/Plugins":
				{
					return new ModeInfo(
						"pluginPreset",
						"Open plugin preset", "Save plugin preset",
						FileDialogFeature.GetPresetExtensions(true));
				}

				case "Custom/Atom/Person/General":
				{
					return new ModeInfo(
						"generalPreset",
						"Open general preset", "Save general preset",
						FileDialogFeature.GetPresetExtensions(true));
				}

				case "Custom/Atom/Person/Appearance":
				{
					return new ModeInfo(
						"appearancePreset",
						"Open appearance preset", "Save appearance preset",
						FileDialogFeature.GetPresetExtensions(true));
				}

				case "Custom/Atom/Person/Pose":
				{
					return new ModeInfo(
						"posePreset",
						"Open pose preset", "Save pose preset",
						FileDialogFeature.GetPresetExtensions(true));
				}

				case "Custom/Atom/Person/BreastPhysics":
				{
					return new ModeInfo(
						"breastPhysicsPreset",
						"Open breast physics preset",
						"Save breast physics preset",
						FileDialogFeature.GetPresetExtensions(true));
				}

				case "Custom/Atom/Person/GlutePhysics":
				{
					return new ModeInfo(
						"glutePhysicsPreset",
						"Open glute physics preset",
						"Save glute physics preset",
						FileDialogFeature.GetPresetExtensions(true));
				}

				case "Custom/Atom/Person/AnimationPresets":
				{
					return new ModeInfo(
						"animationPreset",
						"Open animation preset",
						"Save animation preset",
						FileDialogFeature.GetPresetExtensions(true));
				}

				case "Custom/Atom/Person/Clothing":
				{
					return new ModeInfo(
						"clothingPreset",
						"Open clothing preset",
						"Save clothing preset",
						FileDialogFeature.GetPresetExtensions(true));
				}

				case "Custom/Atom/Person/Hair":
				{
					return new ModeInfo(
						"hairPreset",
						"Open hair preset",
						"Save hair preset",
						FileDialogFeature.GetPresetExtensions(true));
				}

				case "Custom/Atom/Person/Morphs":
				{
					return new ModeInfo(
						"morphPreset",
						"Open morphs preset",
						"Save morphs preset",
						FileDialogFeature.GetPresetExtensions(true));
				}

				case "Custom/Atom/Person/Skin":
				{
					return new ModeInfo(
						"skinPreset",
						"Open skin preset",
						"Save skin preset",
						FileDialogFeature.GetPresetExtensions(true));
				}


				case "Saves/Person/full":
				{
					return new ModeInfo(
						"legacyPreset",
						"Open preset (legacy)", "Save preset (legacy)",
						FileDialogFeature.GetLegacyPresetExtensions(true));
				}

				case "Saves/Person/appearance":
				{
					return new ModeInfo(
						"legacyAppearancePreset",
						"Open appearance preset (legacy)",
						"Save appearance preset (legacy)",
						FileDialogFeature.GetLegacyPresetExtensions(true));
				}

				case "Saves/Person/pose":
				{
					return new ModeInfo(
						"legacyPosePreset",
						"Open pose preset (legacy)",
						"Save pose preset (legacy)",
						FileDialogFeature.GetLegacyPresetExtensions(true));
				}

				default:
				{
					AlternateUI.Instance.Log.Error($"unknown preset '{path}'");

					return new ModeInfo(
						"unknownPreset", "Open preset", "Save preset",
						FileDialogFeature.GetPresetExtensions(true));
				}
			}
		}

		private static ModeInfo GetTextureInfo(string path)
		{
			path = path.Replace("\\", "/");

			return new ModeInfo(
				"texture",
				"Open texture", "Save texture",
				FileDialogFeature.GetTextureExtensions(true));
		}
	}
}
