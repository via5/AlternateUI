using MVR.FileManagementSecure;
using System.Collections.Generic;

namespace AUI.FileDialog
{
	interface IFileContainer
	{
		bool Virtual { get; }
		string Path { get; }
		string Search { get; set; }
		string[] Extensions { get; set; }

		List<File> GetFiles(FileDialog fd);
	}


	abstract class BasicFileContainer : IFileContainer
	{
		private readonly string path_;
		private string search_ = "";
		private string[] exts_ = null;

		protected BasicFileContainer(string path)
		{
			path_ = path;
		}

		public abstract bool Virtual { get; }

		public string Path
		{
			get { return path_; }
		}

		public string Search
		{
			get { return search_; }
			set { search_ = value; }
		}

		public string[] Extensions
		{
			get { return exts_; }
			set { exts_ = value; }
		}

		protected Cache.Filter GetFilter()
		{
			return new Cache.Filter(search_, exts_);
		}

		public abstract List<File> GetFiles(FileDialog fd);
	}


	class EmptyContainer : BasicFileContainer
	{
		public EmptyContainer()
			: base("")
		{
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override List<File> GetFiles(FileDialog fd)
		{
			return new List<File>();
		}
	}


	class DirectoryContainer : BasicFileContainer
	{
		public DirectoryContainer(string path)
			: base(path)
		{
		}

		public override bool Virtual
		{
			get { return false; }
		}

		public override List<File> GetFiles(FileDialog fd)
		{
			return GetFiles(fd.FlattenDirectories);
		}

		protected List<File> GetFiles(bool flatten)
		{
			if (string.IsNullOrEmpty(Path))
				return new List<File>();

			List<File> list;

			if (flatten)
				list = Cache.GetFilesRecursive(Path, GetFilter());
			else
				list = Cache.GetFiles(Path, GetFilter());

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


	class PackagesFlatContainer : BasicFileContainer
	{
		public PackagesFlatContainer()
			: base("Packages flattened")
		{
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override List<File> GetFiles(FileDialog fd)
		{
			return Cache.GetPackagesFlat(Cache.ScenesRoot, GetFilter());
		}
	}


	class AllFlatContainer : BasicFileContainer
	{
		public AllFlatContainer()
			: base("All flattened")
		{
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override List<File> GetFiles(FileDialog fd)
		{
			var list = new List<File>();

			list.AddRange(Cache.GetPackagesFlat(Cache.ScenesRoot, GetFilter()));
			list.AddRange(Cache.GetFilesRecursive(Cache.ScenesRoot, GetFilter()));

			return list;
		}

	}
}
