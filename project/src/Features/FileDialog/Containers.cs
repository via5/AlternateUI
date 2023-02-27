using MVR.FileManagementSecure;
using System.Collections.Generic;

namespace AUI.FileDialog
{/*
	interface IFileContainer
	{
		bool Virtual { get; }
		string Path { get; }
		string Search { get; set; }
		string[] Extensions { get; set; }
		int Sort { get; set; }
		int SortDirection { get; set; }

		List<IFile> GetFiles(FileDialog fd);
	}


	abstract class BasicFileContainer : IFileContainer
	{
		private readonly string path_;
		private string search_ = "";
		private string[] exts_ = null;
		private int sort_ = Filter.SortFilename;
		private int sortDir_ = Filter.SortAscending;

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

		public int Sort
		{
			get { return sort_; }
			set { sort_ = value; }
		}

		public int SortDirection
		{
			get { return sortDir_; }
			set { sortDir_ = value; }
		}

		protected Filter GetFilter()
		{
			return new Filter(search_, exts_, sort_, sortDir_);
		}

		public List<IFile> GetFiles(FileDialog fd)
		{
			var list = new List<IFile>();
			DoGetFiles(fd, list, GetFilter());
			return list;
		}

		public abstract void DoGetFiles(
			FileDialog fd, List<IFile> list, Filter filter);
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
			FileDialog fd, List<IFile> list, Filter filter)
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
			FileDialog fd, List<IFile> list, Filter filter)
		{
			GetFiles(fd.FlattenDirectories, list, filter);
		}

		protected void GetFiles(
			bool flatten, List<IFile> list, Filter filter)
		{
			if (string.IsNullOrEmpty(Path))
				return;

			if (flatten)
				list.AddRange(FS.GetFilesRecursive(Path, filter));
			else
				list.AddRange(FS.GetFiles(Path, filter));
		}
	}


	class PackageContainer : DirectoryContainer
	{
		private readonly IFile p_;

		public PackageContainer(IFile p)
			: base(p.Path)
		{
			p_ = p;
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override void DoGetFiles(
			FileDialog fd, List<IFile> list, Filter filter)
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
			FileDialog fd, List<IFile> list, Filter filter)
		{
			if (always_ || fd.FlattenDirectories)
				list.AddRange(FS.GetPackageFilesRecursive(filter));
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
			FileDialog fd, List<IFile> list, Filter filter)
		{
			if (always_ || fd.FlattenDirectories)
			{
				//list.AddRange(FS.GetPackageFilesRecursive(filter));
				//list.AddRange(FS.GetFilesRecursive(FS.SavesRoot, filter));
				list.AddRange(FS.GetFilesRecursive(FS.Root, filter));
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
			FileDialog fd, List<IFile> list, Filter filter)
		{
			if (fd.FlattenDirectories)
			{
				foreach (var c in containers_)
					c.DoGetFiles(fd, list, filter);
			}
		}
	}*/
}
