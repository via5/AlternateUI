using MVR.FileManagementSecure;

namespace AUI.FileDialog
{
	interface IFileDialogMode
	{
		bool GetDefaultFlattenDirectories();
		string GetCurrentDirectory();
		string GetTitle();
		ExtensionItem[] GetExtensions();
		string GetActionText();
		bool IsWritable();
		bool CanExecute(FileDialog fd);
		void Execute(FileDialog fd);
		string GetPath(FileDialog fd);
	}


	abstract class BasicMode : IFileDialogMode
	{
		private readonly string title_;
		private readonly ExtensionItem[] exts_;
		private string lastPath_ = "";
		private bool defaultFlatten_ = true;

		protected BasicMode(
			string title, ExtensionItem[] exts,
			string defaultPath, bool defaultFlatten)
		{
			title_ = title;
			exts_ = exts;
			lastPath_ = defaultPath;
			defaultFlatten_ = defaultFlatten;
		}

		public virtual bool GetDefaultFlattenDirectories()
		{
			return defaultFlatten_;
		}

		public virtual string GetCurrentDirectory()
		{
			return lastPath_;
		}

		public virtual string GetTitle()
		{
			return title_;
		}

		public virtual ExtensionItem[] GetExtensions()
		{
			return exts_;
		}

		public abstract string GetActionText();
		public abstract bool IsWritable();
		public abstract bool CanExecute(FileDialog fd);
		public abstract string GetPath(FileDialog fd);

		public virtual void Execute(FileDialog fd)
		{
			lastPath_ = fd.SelectedDirectory.VirtualPath;
		}
	}


	class NoMode : BasicMode
	{
		public NoMode()
			: base("", new ExtensionItem[0], "", false)
		{
		}

		public override string GetActionText()
		{
			return "";
		}

		public override bool IsWritable()
		{
			return false;
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
		public OpenMode(string title, ExtensionItem[] exts, string defaultPath, bool defaultFlatten)
			: base(title, exts, defaultPath, defaultFlatten)
		{
		}

		public override string GetActionText()
		{
			return "Open";
		}

		public override bool IsWritable()
		{
			return false;
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
		public SaveMode(string title, ExtensionItem[] exts, string defaultPath)
			: base(title, exts, defaultPath, false)
		{
		}

		public override string GetActionText()
		{
			return "Save";
		}

		public override bool IsWritable()
		{
			return true;
		}

		public override bool CanExecute(FileDialog fd)
		{
			return (GetPath(fd) != "");
		}

		public override void Execute(FileDialog fd)
		{
			base.Execute(fd);
			fd.SelectedDirectory?.ClearCache();
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
		public static IFileDialogMode OpenScene()
		{
			return new OpenMode(
				"Open scene", FileDialogFeature.GetSceneExtensions(true),
				"VaM/Saves/scene", true);
		}

		public static IFileDialogMode SaveScene()
		{
			return new SaveMode(
				"Save scene", FileDialogFeature.GetSceneExtensions(false),
				"VaM/Saves/scene");
		}

		public static IFileDialogMode OpenCUA()
		{
			return new OpenMode(
				"Open asset bundle", FileDialogFeature.GetCUAExtensions(true),
				"VaM/Custom/Assets", true);
		}

		public static IFileDialogMode OpenPlugin()
		{
			return new OpenMode(
				"Open plugin", FileDialogFeature.GetPluginExtensions(true),
				"VaM/Custom/Scripts", false);
		}
	}
}
