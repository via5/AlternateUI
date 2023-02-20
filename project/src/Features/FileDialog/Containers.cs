using MVR.FileManagementSecure;
using System.Collections.Generic;

namespace AUI.FileDialog
{
	interface IFileContainer
	{
		bool Virtual { get; }
		string Path { get; }
		List<File> GetFiles(FileDialog fd);
	}


	class EmptyContainer : IFileContainer
	{
		public bool Virtual
		{
			get { return true; }
		}

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
		public abstract bool Virtual { get; }
		public abstract string Path { get; }
		public abstract List<File> GetFiles(FileDialog fd);

		protected void GetFilesRecursive(string parent, List<File> list)
		{
			list.AddRange(GetFiles(parent));

			foreach (var d in Cache.GetDirectories(parent))
				GetFilesRecursive(d.Path, list);
		}

		protected List<File> GetFiles(string parent)
		{
			return Cache.GetFiles(parent, Cache.SceneExtensions);
		}
	}


	class DirectoryContainer : FSContainer
	{
		private readonly string path_;

		public DirectoryContainer(string path)
		{
			path_ = path;
		}

		public override bool Virtual
		{
			get { return false; }
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

		public override bool Virtual
		{
			get { return true; }
		}

		public override List<File> GetFiles(FileDialog fd)
		{
			return GetFiles(fd.FlattenPackages);
		}
	}


	class PackagesFlatContainer : FSContainer
	{
		public override bool Virtual
		{
			get { return true; }
		}

		public override string Path
		{
			get { return "Packages flattened"; }
		}

		public override List<File> GetFiles(FileDialog fd)
		{
			return Cache.GetPackagesFlat(Cache.SceneExtensions);
		}
	}


	class AllFlatContainer : FSContainer
	{
		public override bool Virtual
		{
			get { return true; }
		}

		public override string Path
		{
			get { return "All flattened"; }
		}

		public override List<File> GetFiles(FileDialog fd)
		{
			var list = new List<File>();

			list.AddRange(Cache.GetPackagesFlat(Cache.SceneExtensions));
			GetFilesRecursive("Saves/scene", list);

			return list;
		}

	}
}
