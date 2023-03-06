using MVR.FileManagementSecure;
using SimpleJSON;
using System.Collections.Generic;

namespace AUI.FileDialog
{
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

			paths_.Add(s);
			++index_;
		}
	}


	class Options
	{
		private string name_;
		private string lastPath_;
		private bool flattenDirs_;
		private bool flattenPackages_;
		private bool mergePackages_;
		private bool showHiddenFolders_;
		private bool showHiddenFiles_;
		private int sort_, sortDir_;
		private readonly History history_ = new History();

		private static readonly Dictionary<string, Options> map_ =
			new Dictionary<string, Options>();

		public Options(
			string name, string lastPath,
			bool flattenDirs, bool flattenPackages, bool mergePackages,
			bool showHiddenFolders, bool showHiddenFiles, int sort, int sortDir)
		{
			name_ = name;
			lastPath_ = lastPath;
			flattenDirs_ = flattenDirs;
			flattenPackages_ = flattenPackages;
			mergePackages_ = mergePackages;
			showHiddenFolders_ = showHiddenFolders;
			showHiddenFiles_ = showHiddenFiles;
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

		public string CurrentDirectory
		{
			get { return lastPath_; }
			set { lastPath_ = value; Changed(); }
		}

		public void Load()
		{
			var path = GetOptionsFile();
			if (string.IsNullOrEmpty(path) || !FileManagerSecure.FileExists(path))
				return;

			var j = SuperController.singleton.LoadJSON(path)?.AsObject;
			if (j == null)
				return;

			if (j.HasKey("lastPath"))
				lastPath_ = j["lastPath"].Value;

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

			j["lastPath"] = new JSONData(lastPath_);
			j["flattenDirectories"] = new JSONData(flattenDirs_);
			j["flattenPackages"] = new JSONData(flattenPackages_);
			j["mergePackages"] = new JSONData(mergePackages_);
			j["showHiddenFolders"] = new JSONData(showHiddenFolders_);
			j["showHiddenFiles"] = new JSONData(showHiddenFiles_);
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

		string Title { get; }
		ExtensionItem[] Extensions { get; }
		string DefaultDirectory { get; }
		string ActionText { get; }
		bool IsWritable { get; }

		bool CanExecute(FileDialog fd);
		void Execute(FileDialog fd, ExecuteHandler h);
		string GetPath(FileDialog fd);
	}


	abstract class BasicMode : IFileDialogMode
	{
		private readonly string name_;
		private readonly string title_;
		private readonly ExtensionItem[] exts_;
		private readonly string defaultPath_;
		private readonly Options opts_;

		protected BasicMode(
			string name, string title, ExtensionItem[] exts, string defaultPath,
			bool flattenDirs, bool flattenPackages, bool mergePackages,
			bool showHiddenFolders, bool showHiddenFiles, int sort, int sortDir)
		{
			name_ = name;
			title_ = title;
			exts_ = exts;
			defaultPath_ = defaultPath;

			opts_ = Options.Get(name_);
			if (opts_ == null)
			{
				opts_ = new Options(
					name_, defaultPath, flattenDirs, flattenPackages,
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
		}

		public ExtensionItem[] Extensions
		{
			get { return exts_; }
		}

		public string DefaultDirectory
		{
			get { return defaultPath_; }
		}

		public abstract string ActionText { get; }
		public abstract bool IsWritable { get; }

		public abstract bool CanExecute(FileDialog fd);
		public abstract string GetPath(FileDialog fd);

		public void Execute(FileDialog fd, ExecuteHandler h)
		{
			opts_.CurrentDirectory = fd.SelectedDirectory.VirtualPath;
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
				  null, "", new ExtensionItem[0], "", false, false, false,
				  false, false, FS.Context.NoSort, FS.Context.NoSortDirection)
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
	}


	class OpenMode : BasicMode
	{
		public OpenMode(
			string name, string title, ExtensionItem[] exts, string defaultPath,
			bool flattenDirs, bool flattenPackages, bool mergePackages,
			bool showHiddenFolders, bool showHiddenFiles, int sort, int sortDir)
				: base(
					  name, title, exts, defaultPath,
					  flattenDirs, flattenPackages, mergePackages,
					  showHiddenFolders, showHiddenFiles, sort, sortDir)
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

					path = Path.Join(dir, file);
				}
			}
			else
			{
				path = s.MakeRealPath();
			}

			if (path == "")
				return "";

			var npath = FileManagerSecure.GetFullPath(path);
			AlternateUI.Instance.Log.Info($"path={path} npath={npath}");

			return npath;
		}
	}


	class SaveMode : BasicMode
	{
		public SaveMode(
			string name, string title, ExtensionItem[] exts, string defaultPath,
			bool flattenDirs, bool flattenPackages, bool mergePackages,
			bool showHiddenFolders, bool showHiddenFiles, int sort, int sortDir)
				: base(
					  name, title, exts, defaultPath,
					  flattenDirs, flattenPackages, mergePackages,
					  showHiddenFolders, showHiddenFiles, sort, sortDir)
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
			if (cwd == null || cwd.Virtual)
				return "";

			var dir = cwd.MakeRealPath()?.Trim() ?? "";
			if (dir == "")
				return "";

			var file = fd.Filename?.Trim() ?? "";
			if (file == "")
				return "";

			if (file.IndexOf('.') == -1)
				file += fd.GetDefaultExtension();

			return Path.Join(dir, file);
		}
	}


	static class Modes
	{
		static IFileDialogMode openScene_ = null;
		static IFileDialogMode saveScene_ = null;
		static IFileDialogMode openCUA_ = null;
		static IFileDialogMode openPlugin_ = null;

		public static IFileDialogMode OpenScene()
		{
			if (openScene_ == null)
			{
				openScene_ = new OpenMode(
					"scene", "Open scene",
					FileDialogFeature.GetSceneExtensions(true),
					"VaM/Saves/scene", true, true, true, false, false,
					FS.Context.SortDateCreated, FS.Context.SortDescending);
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
					"VaM/Saves/scene", false, false, false, false, false,
					FS.Context.SortDateCreated, FS.Context.SortDescending);
			}

			return saveScene_;
		}

		public static IFileDialogMode OpenCUA()
		{
			if (openCUA_ == null)
			{
				openCUA_ = new OpenMode(
					"openCUA", "Open asset bundle",
					FileDialogFeature.GetCUAExtensions(true),
					"VaM/Custom/Assets", true, true, true, false, false,
					FS.Context.SortDateCreated, FS.Context.SortDescending);
			}

			return openCUA_;
		}

		public static IFileDialogMode OpenPlugin()
		{
			if (openPlugin_ == null)
			{
				openPlugin_ = new OpenMode(
					"openPlugin", "Open plugin",
					FileDialogFeature.GetPluginExtensions(true),
					"VaM/Custom/Scripts", false, true, true, false, false,
					FS.Context.SortDateCreated, FS.Context.SortDescending);
			}

			return openPlugin_;
		}
	}
}
