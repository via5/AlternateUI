using MVR.FileManagementSecure;
using System.Collections.Generic;

namespace AUI.FileDialog
{
	interface IFileContainer
	{
		string Path { get; }
		List<File> GetFiles(FileDialog fd);
	}


	class EmptyContainer : IFileContainer
	{
		public string Path
		{
			get { return ""; }
		}

		public List<File> GetFiles(FileDialog fd)
		{
			return new List<File>();
		}
	}


	abstract class FSContainer : IFileContainer
	{
		public abstract string Path { get; }
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

		public override string Path
		{
			get { return path_; }
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
		public override string Path
		{
			get { return "Packages flattened"; }
		}

		public override List<File> GetFiles(FileDialog fd)
		{
			return GetPackagesFlat();
		}
	}


	class AllFlatContainer : FSContainer
	{
		public override string Path
		{
			get { return "All flattened"; }
		}

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
}
