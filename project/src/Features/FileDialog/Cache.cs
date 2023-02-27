using MVR.FileManagementSecure;
using System.Collections.Generic;

namespace AUI.FileDialog
{
	using FMS = FileManagerSecure;

	class Filesystem
	{
		public struct Extension
		{
			public string name;
			public string ext;

			public Extension(string name, string ext)
			{
				this.name = name;
				this.ext = ext;
			}
		}


		public static string SavesRoot = "Saves";

		public static string DefaultSceneExtension = ".json";
		public static Extension[] SceneExtensions = new Extension[]
		{
			new Extension("Scenes", ".json"),
			new Extension("VAC files", ".vac"),
			new Extension("Zip files", ".zip"),
		};


		private readonly Dictionary<string, IFilesystemContainer> dirs_ =
			new Dictionary<string, IFilesystemContainer>();

		private readonly IFilesystemContainer root_;
		private readonly IFilesystemContainer packagesRoot_;


		public Filesystem()
		{
			root_ = new RootDirectory(this);
			packagesRoot_ = new PackageRootDirectory(this, root_);
		}
		/*
		public bool HasSubDirectories(string path, Filter filter)
		{
			return GetDirectory(path).HasSubDirectories(filter);
		}

		public List<IFilesystemObject> GetFiles(string path, Filter filter)
		{
			return GetDirectory(path).GetFiles(filter);
		}

		public List<IFilesystemObject> GetFilesRecursive(string path, Filter filter)
		{
			return GetDirectory(path).GetFilesRecursive(filter);
		}

		public List<IFilesystemObject> GetDirectories(string path, Filter filter)
		{
			return GetDirectory(path).GetSubDirectories(filter);
		}

		public List<IFilesystemObject> GetPackages(Filter filter)
		{
			return GetDirectory(FS.PackageRoot).GetSubDirectories(filter);
		}
		*/
		public IPackage GetPackage(string name)
		{
			foreach (var f in packagesRoot_.GetSubDirectories(null))
			{
				if (f.Name == name)
					return f as IPackage;
			}

			return null;
		}

		public bool DirectoryInPackage(string path)
		{
			return FMS.IsDirectoryInPackage(path);
		}

		public IFilesystemContainer GetRootDirectory()
		{
			return root_;
		}

		public IFilesystemContainer GetPackagesRootDirectory()
		{
			return packagesRoot_;
		}
		/*
		public IFilesystemContainer GetDirectory2(string path)
		{
			IFilesystemContainer d;

			if (!dirs_.TryGetValue(path, out d))
			{
				d = new FSDirectory(this, path);
				dirs_.Add(path, d);
			}

			return d;
		}*/
	}


	static class FS
	{
		private static readonly Filesystem fs_ = new Filesystem();

		public static Filesystem Instance
		{
			get { return fs_; }
		}
		/*
		public static bool HasSubDirectories(string path, Filter filter)
		{
			return fs_.HasSubDirectories(path, filter);
		}

		public static List<IFilesystemObject> GetFiles(string path, Filter f)
		{
			return fs_.GetFiles(path, f);
		}

		public static List<IFilesystemObject> GetFilesRecursive(string path, Filter f)
		{
			return fs_.GetFilesRecursive(path, f);
		}

		public static List<IFilesystemObject> GetDirectories(string path, Filter f)
		{
			return fs_.GetDirectories(path, f);
		}


		public static bool HasPackages(Filter f)
		{
			return fs_.HasSubDirectories(PackageRoot, f);
		}

		public static List<IFilesystemObject> GetPackages(Filter f)
		{
			return fs_.GetPackages(f);
		}

		public static List<IFilesystemObject> GetPackageFilesRecursive(Filter f)
		{
			return fs_.GetFilesRecursive(PackageRoot, f);
		}

		public static IFilesystemObject GetPackage(string path)
		{
			return fs_.GetPackage(path);
		}

		public static bool DirectoryInPackage(string path)
		{
			return fs_.DirectoryInPackage(path);
		}*/
	}
}
