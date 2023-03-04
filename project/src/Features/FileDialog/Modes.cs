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


	interface IFileDialogMode
	{
		History History { get; }
		bool FlattenDirectories { get; set; }
		bool FlattenPackages { get; set; }
		int Sort { get; set; }
		int SortDirection { get; set; }
		string CurrentDirectory { get; set; }
		string DefaultDirectory { get; }

		string Title { get; }
		ExtensionItem[] Extensions { get; }
		string ActionText { get; }
		bool IsWritable { get; }

		bool CanExecute(FileDialog fd);
		void Execute(FileDialog fd);
		string GetPath(FileDialog fd);
	}


	abstract class BasicMode : IFileDialogMode
	{
		private readonly string name_;
		private readonly string title_;
		private readonly ExtensionItem[] exts_;
		private readonly string defaultPath_;
		private string lastPath_;
		private bool flattenDirs_ = true;
		private bool flattenPackages_ = true;
		private int sort_, sortDir_;
		private readonly History history_ = new History();

		protected BasicMode(
			string name, string title, ExtensionItem[] exts, string defaultPath,
			bool flattenDirs, bool flattenPackages, int sort, int sortDir)
		{
			name_ = name;
			title_ = title;
			exts_ = exts;
			defaultPath_ = defaultPath;
			lastPath_ = defaultPath;
			flattenDirs_ = flattenDirs;
			flattenPackages_ = flattenPackages;
			sort_ = sort;
			sortDir_ = sortDir;

			if (!string.IsNullOrEmpty(name_))
				Load();
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

		public string DefaultDirectory
		{
			get { return defaultPath_; }
		}

		public string CurrentDirectory
		{
			get { return lastPath_; }
			set { lastPath_ = value; Changed(); }
		}


		public string Title
		{
			get { return title_; }
		}

		public ExtensionItem[] Extensions
		{
			get { return exts_; }
		}

		public abstract string ActionText { get; }
		public abstract bool IsWritable { get; }

		public abstract bool CanExecute(FileDialog fd);
		public abstract string GetPath(FileDialog fd);

		public void Execute(FileDialog fd)
		{
			lastPath_ = fd.SelectedDirectory.VirtualPath;
			Changed();
		}

		protected virtual void DoExecute(FileDialog fd)
		{
			// no-op
		}

		protected void Changed()
		{
			Save();
		}

		private string GetOptionsFile()
		{
			return AlternateUI.Instance.GetConfigFilePath(
				$"aui.filedialog.modes.{name_}.json");
		}

		private void Load()
		{
			var f = GetOptionsFile();
			if (!FileManagerSecure.FileExists(f))
				return;

			var j = SuperController.singleton.LoadJSON(f)?.AsObject;
			if (j == null)
				return;

			if (j.HasKey("flattenDirectories"))
				flattenDirs_ = j["flattenDirectories"].AsBool;

			if (j.HasKey("flattenPackages"))
				flattenPackages_ = j["flattenPackages"].AsBool;

			if (j.HasKey("sort"))
				sort_ = j["sort"].AsInt;

			if (j.HasKey("sortDirection"))
				sort_ = j["sortDirection"].AsInt;

			if (j.HasKey("path"))
				lastPath_ = j["path"].Value;
		}

		private void Save()
		{
			var j = new JSONClass();

			j["flattenDirectories"] = new JSONData(flattenDirs_);
			j["flattenPackages"] = new JSONData(flattenPackages_);
			j["sort"] = new JSONData(sort_);
			j["sortDirection"] = new JSONData(sortDir_);
			j["path"] = new JSONData(lastPath_);

			SuperController.singleton.SaveJSON(j, GetOptionsFile());
		}
	}


	class NoMode : BasicMode
	{
		public NoMode()
			: base(
				  null, "", new ExtensionItem[0], "", false, false,
				  FS.Filter.NoSort, FS.Filter.NoSortDirection)
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
			bool flattenDirs, bool flattenPackages, int sort, int sortDir)
				: base(name, title, exts, defaultPath, flattenDirs, flattenPackages, sort, sortDir)
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
			bool flattenDirs, bool flattenPackages, int sort, int sortDir)
				: base(name, title, exts, defaultPath, flattenDirs, flattenPackages, sort, sortDir)
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

		protected override void DoExecute(FileDialog fd)
		{
			fd.SelectedDirectory?.ClearCache();
			fd.SelectedFile?.ClearCache();
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
					"openScene", "Open scene",
					FileDialogFeature.GetSceneExtensions(true),
					"VaM/Saves/scene", true, true,
					FS.Filter.SortDateCreated, FS.Filter.SortDescending);
			}

			return openScene_;
		}

		public static IFileDialogMode SaveScene()
		{
			if (saveScene_ == null)
			{
				saveScene_ = new SaveMode(
					"saveScene", "Save scene",
					FileDialogFeature.GetSceneExtensions(false),
					"VaM/Saves/scene", true, true,
					FS.Filter.SortDateCreated, FS.Filter.SortDescending);
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
					"VaM/Custom/Assets", true, true,
					FS.Filter.SortDateCreated, FS.Filter.SortDescending);
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
					"VaM/Custom/Scripts", false, true,
					FS.Filter.SortDateCreated, FS.Filter.SortDescending);
			}

			return openPlugin_;
		}
	}
}
