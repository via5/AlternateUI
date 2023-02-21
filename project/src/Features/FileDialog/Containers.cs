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

		public List<File> GetFiles(FileDialog fd)
		{
			var list = new List<File>();
			DoGetFiles(fd, list, GetFilter());
			return list;
		}

		public abstract void DoGetFiles(
			FileDialog fd, List<File> list, Cache.Filter filter);
	}


	class EmptyContainer : BasicFileContainer
	{
		public EmptyContainer(string path)
			: base(path)
		{
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override void DoGetFiles(
			FileDialog fd, List<File> list, Cache.Filter filter)
		{
			// no-op
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

		public override void DoGetFiles(
			FileDialog fd, List<File> list, Cache.Filter filter)
		{
			GetFiles(fd.FlattenDirectories, list, filter);
		}

		protected void GetFiles(
			bool flatten, List<File> list, Cache.Filter filter)
		{
			if (string.IsNullOrEmpty(Path))
				return;

			if (flatten)
				list.AddRange(Cache.GetFilesRecursive(Path, filter));
			else
				list.AddRange(Cache.GetFiles(Path, filter));
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

		public override void DoGetFiles(
			FileDialog fd, List<File> list, Cache.Filter filter)
		{
			GetFiles(fd.FlattenPackages, list, filter);
		}
	}


	class PackagesFlatContainer : BasicFileContainer
	{
		private readonly bool always_;

		public PackagesFlatContainer(bool always)
			: base("Packages flattened")
		{
			always_ = always;
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override void DoGetFiles(
			FileDialog fd, List<File> list, Cache.Filter filter)
		{
			if (always_ || fd.FlattenDirectories)
				list.AddRange(Cache.GetPackagesFlat(Cache.ScenesRoot, filter));
		}
	}


	class AllFlatContainer : BasicFileContainer
	{
		private readonly bool always_;

		public AllFlatContainer(string text, bool always)
			: base(text)
		{
			always_ = always;
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override void DoGetFiles(
			FileDialog fd, List<File> list, Cache.Filter filter)
		{
			if (always_ || fd.FlattenDirectories)
			{
				list.AddRange(Cache.GetPackagesFlat(Cache.ScenesRoot, filter));
				list.AddRange(Cache.GetFilesRecursive(Cache.ScenesRoot, filter));
			}
		}
	}


	class MergedContainer : BasicFileContainer
	{
		private readonly List<BasicFileContainer> containers_ =
			new List<BasicFileContainer>();

		public MergedContainer(string text)
			: base(text)
		{
		}

		public void Add(BasicFileContainer c)
		{
			containers_.Add(c);
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override void DoGetFiles(
			FileDialog fd, List<File> list, Cache.Filter filter)
		{
			foreach (var c in containers_)
				c.DoGetFiles(fd, list, filter);
		}
	}
}
