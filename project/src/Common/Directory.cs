using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;

namespace AUI.FileDialog
{
	using FMS = FileManagerSecure;


	interface IFilesystemObject
	{
		IFilesystemContainer Parent { get; }

		string Name { get; }
		string VirtualPath { get; }
		string DisplayName { get; }
		DateTime DateCreated { get; }
		DateTime DateModified { get; }
		Icon Icon { get; }

		bool CanPin { get; }
		bool Virtual { get; }
		bool IsFlattened { get; }
		IPackage ParentPackage { get; }

		string MakeRealPath();
	}


	interface IFilesystemContainer : IFilesystemObject
	{
		bool HasSubDirectories(Filter filter);
		List<IFilesystemContainer> GetSubDirectories(Filter filter);
		List<IFilesystemObject> GetFiles(Filter filter);
		List<IFilesystemObject> GetFilesRecursive(Filter filter);
	}


	interface IDirectory : IFilesystemContainer
	{
	}

	interface IPackage : IFilesystemContainer
	{
	}

	interface IFile : IFilesystemObject
	{
	}


	abstract class BasicFilesystemContainer : IFilesystemContainer
	{
		protected class Cache<EntriesType> where EntriesType : IFilesystemObject
		{
			public List<EntriesType> entries = null;

			public string currentExtensions = "";
			public List<EntriesType> perExtension = null;

			public string currentSearch = "";
			public List<EntriesType> searched = null;

			public int currentSort = Filter.NoSort;
			public int currentSortDir = Filter.NoSortDirection;
			public List<EntriesType> sorted = null;
		}

		protected class Caches
		{
			public Cache<IFilesystemObject> files = new Cache<IFilesystemObject>();
			public Cache<IFilesystemContainer> dirs = new Cache<IFilesystemContainer>();
		}


		protected readonly Filesystem fs_;
		private readonly IFilesystemContainer parent_;
		private readonly string name_;
		protected Caches c_ = new Caches();
		protected Caches rc_ = new Caches();


		public BasicFilesystemContainer(Filesystem fs, IFilesystemContainer parent, string name)
		{
			fs_ = fs;
			parent_ = parent;
			name_ = name;
		}

		public IFilesystemContainer Parent
		{
			get { return parent_; }
		}

		public string Name
		{
			get { return name_; }
		}

		public string VirtualPath
		{
			get
			{
				string s = Name;

				var parent = parent_;
				while (parent != null)
				{
					s = parent.Name + "/" + s;
					parent = parent.Parent;
				}

				return s;
			}
		}

		public abstract string MakeRealPath();

		public virtual string DisplayName
		{
			get { return Name; }
		}

		public abstract DateTime DateCreated { get; }
		public abstract DateTime DateModified { get; }
		public abstract Icon Icon { get; }

		public abstract bool CanPin { get; }
		public abstract bool Virtual { get; }
		public abstract bool IsFlattened { get; }

		public IPackage ParentPackage
		{
			get
			{
				IFilesystemObject o = this;

				while (o != null)
				{
					if (o is IPackage)
						return o as IPackage;

					o = o.Parent;
				}

				return null;
			}
		}

		public bool HasSubDirectories(Filter filter)
		{
			return (GetSubDirectories(filter).Count > 0);
		}

		public List<IFilesystemContainer> GetSubDirectories(Filter filter)
		{
			return GetSubDirectoriesImpl(filter);
		}

		protected abstract List<IFilesystemContainer> GetSubDirectoriesImpl(Filter filter);

		public List<IFilesystemObject> GetFiles(Filter filter)
		{
			return GetFilesImpl(c_, filter);
		}

		protected abstract List<IFilesystemObject> GetFilesImpl(Caches c, Filter filter);

		public List<IFilesystemObject> GetFilesRecursive(Filter filter)
		{
			if (rc_.files.entries == null)
			{
				rc_.files.entries = new List<IFilesystemObject>();
				GetFilesRecursiveImpl(filter);
			}

			return FilterCaches(rc_.files, filter);
		}

		protected abstract void GetFilesRecursiveImpl(Filter filter);


		protected List<IFilesystemContainer> DoGetSubDirectories(Caches c, Filter filter)
		{
			if (c.dirs.entries == null)
			{
				c.dirs.entries = new List<IFilesystemContainer>();

				foreach (var dirPath in GetDirectoriesFromFMS(MakeRealPath()))
				{
					var name = Path.Filename(dirPath);
					c.dirs.entries.Add(new FSDirectory(fs_, this, name));
				}
			}

			return FilterCaches(c.dirs, filter);
		}

		internal void DoGetFilesRecursive(List<IFilesystemObject> list)
		{
			list.AddRange(GetFilesImpl(c_, null));

			foreach (var sd in GetSubDirectories(null))
				(sd as BasicFilesystemContainer).DoGetFilesRecursive(list);
		}

		protected List<IFilesystemObject> DoGetFiles(Caches c, Filter filter)
		{
			if (c.files.entries == null)
			{
				c.files.entries = new List<IFilesystemObject>();

				foreach (var filePath in GetFilesFromFMS(MakeRealPath()))
				{
					var name = Path.Filename(filePath);

					if (filter == null || filter.Matches(name))
						c.files.entries.Add(new File(this, name));
				}
			}

			if (filter == null)
				return c.files.entries;
			else
				return FilterCaches(c.files, filter);
		}

		protected List<EntryType> FilterCaches<EntryType>(Cache<EntryType> c, Filter filter)
			where EntryType : IFilesystemObject
		{
			if (filter == null)
				return c.entries;

			var list = c.entries;

			if (c.currentExtensions != filter.ExtensionsString)
			{
				c.currentExtensions = filter.ExtensionsString;
				c.currentSearch = null;
				c.currentSort = Filter.NoSort;
				c.currentSortDir = Filter.NoSortDirection;

				if (filter.ExtensionsString != "")
				{
					if (c.perExtension == null)
						c.perExtension = new List<EntryType>();

					c.perExtension.Clear();

					foreach (var f in list)
					{
						if (filter.ExtensionMatches(f.DisplayName))
							c.perExtension.Add(f);
					}

					list = c.perExtension;
				}
			}
			else if (filter.ExtensionsString != "")
			{
				list = c.perExtension;
			}

			if (c.currentSearch != filter.Search)
			{
				c.currentSearch = filter.Search;
				c.currentSort = Filter.NoSort;
				c.currentSortDir = Filter.NoSortDirection;

				if (filter.Search != "")
				{
					if (c.searched == null)
						c.searched = new List<EntryType>();

					c.searched.Clear();

					foreach (var f in list)
					{
						if (filter.SearchMatches(f.DisplayName))
							c.searched.Add(f);
					}

					list = c.searched;
				}
			}
			else if (filter.Search != "")
			{
				list = c.searched;
			}

			return DoSort(c, filter, list);
		}

		protected List<EntryType> DoSort<EntryType>(Cache<EntryType> c, Filter filter, List<EntryType> list)
			where EntryType : IFilesystemObject
		{
			if (c.currentSort != filter.Sort ||
				c.currentSortDir != filter.SortDirection)
			{
				c.currentSort = filter.Sort;
				c.currentSortDir = filter.SortDirection;

				if (filter.Sort != Filter.NoSort)
				{
					if (c.sorted == null)
						c.sorted = new List<EntryType>();

					c.sorted.Clear();
					c.sorted.AddRange(list);

					filter.SortList(c.sorted);
				}

				list = c.sorted;
			}
			else if (filter.Sort != Filter.NoSort)
			{
				list = c.sorted;
			}

			return list;
		}

		private string[] GetDirectoriesFromFMS(string path)
		{
			try
			{
				return FMS.GetDirectories(path);
			}
			catch (Exception)
			{
				AlternateUI.Instance.Log.Error($"bad directory '{path}'");
				return new string[0];
			}
		}

		private string[] GetFilesFromFMS(string path)
		{
			try
			{
				return FMS.GetFiles(path);
			}
			catch (Exception)
			{
				AlternateUI.Instance.Log.Error($"bad directory '{path}'");
				return new string[0];
			}
		}
	}


	class RootDirectory : BasicFilesystemContainer
	{
		private readonly FSDirectory saves_;

		public RootDirectory(Filesystem fs)
			: base(fs, null, "VaM")
		{
			saves_ = new FSDirectory(fs_, this, "Saves");
		}

		public override DateTime DateCreated
		{
			get { return DateTime.MaxValue; }
		}

		public override DateTime DateModified
		{
			get { return DateTime.MaxValue; }
		}

		public override Icon Icon
		{
			get { return Icons.Directory; }
		}

		public override bool CanPin
		{
			get { return false; }
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override bool IsFlattened
		{
			get { return false; }
		}

		public override string MakeRealPath()
		{
			return "";
		}

		protected override List<IFilesystemContainer> GetSubDirectoriesImpl(Filter filter)
		{
			var list = new List<IFilesystemContainer>();
			list.Add(saves_);
			list.Add(fs_.GetPackagesRootDirectory());
			return list;
		}

		protected override List<IFilesystemObject> GetFilesImpl(Caches c, Filter filter)
		{
			return new List<IFilesystemObject>();
		}

		protected override void GetFilesRecursiveImpl(Filter filter)
		{
			DoGetFilesRecursive(rc_.files.entries);
		}
	}


	class FSDirectory : BasicFilesystemContainer, IDirectory
	{
		public FSDirectory(Filesystem fs, IFilesystemContainer parent, string name)
			: base(fs, parent, name)
		{
		}

		public override DateTime DateCreated
		{
			get { return DateTime.MaxValue; }
		}

		public override DateTime DateModified
		{
			get { return DateTime.MaxValue; }
		}

		public override Icon Icon
		{
			get { return Icons.Directory; }
		}

		public override bool CanPin
		{
			get { return true; }
		}

		public override bool Virtual
		{
			get { return false; }
		}

		public override bool IsFlattened
		{
			get { return false; }
		}

		public override string MakeRealPath()
		{
			string s = Name + "/";

			if (Parent != null)
				s = Parent.MakeRealPath() + s;

			return s;
		}

		protected override List<IFilesystemContainer> GetSubDirectoriesImpl(Filter filter)
		{
			return DoGetSubDirectories(c_, filter);
		}

		protected override List<IFilesystemObject> GetFilesImpl(Caches c, Filter filter)
		{
			return DoGetFiles(c, filter);
		}

		protected override void GetFilesRecursiveImpl(Filter filter)
		{
			DoGetFilesRecursive(rc_.files.entries);
		}
	}


	class PackageRootDirectory : BasicFilesystemContainer
	{
		private List<ShortCut> shortCuts_ = null;

		public PackageRootDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Packages")
		{
		}

		public override DateTime DateCreated
		{
			get { return DateTime.MaxValue; }
		}

		public override DateTime DateModified
		{
			get { return DateTime.MaxValue; }
		}

		public override Icon Icon
		{
			get { return Icons.Package; }
		}

		public override bool CanPin
		{
			get { return false; }
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override bool IsFlattened
		{
			get { return false; }
		}

		public override string MakeRealPath()
		{
			return "";
		}

		protected override List<IFilesystemContainer> GetSubDirectoriesImpl(Filter filter)
		{
			if (c_.dirs.entries == null)
			{
				c_.dirs.entries = new List<IFilesystemContainer>();

				foreach (var s in GetShortCuts())
					c_.dirs.entries.Add(new Package(fs_, fs_.GetPackagesRootDirectory(), s));
			}

			// todo filter

			return c_.dirs.entries;
		}

		protected override List<IFilesystemObject> GetFilesImpl(Caches c, Filter filter)
		{
			// no-op
			if (c.files.entries == null)
				c.files.entries = new List<IFilesystemObject>();

			return c.files.entries;
		}

		protected override void GetFilesRecursiveImpl(Filter filter)
		{
			foreach (var d in GetSubDirectories(filter))
				(d as BasicFilesystemContainer).DoGetFilesRecursive(rc_.files.entries);
		}

		private List<ShortCut> GetShortCuts()
		{
			if (shortCuts_ == null)
			{
				shortCuts_ = new List<ShortCut>();

				foreach (var sc in FMS.GetShortCutsForDirectory("Saves/scene"))
				{
					if (string.IsNullOrEmpty(sc.package))
						continue;

					if (!string.IsNullOrEmpty(sc.packageFilter))
						continue;

					shortCuts_.Add(sc);
				}
			}

			return shortCuts_;
		}
	}


	class File : IFile
	{
		private readonly IFilesystemContainer parent_;
		private readonly string name_;

		public File(IFilesystemContainer parent, string name)
		{
			parent_ = parent;
			name_ = name;
		}

		public IFilesystemContainer Parent
		{
			get { return parent_; }
		}

		public string Name
		{
			get { return name_; }
		}

		public string VirtualPath
		{
			get
			{
				string s = name_;

				var parent = Parent;
				while (parent != null)
				{
					s = parent.Name + "/" + s;
					parent = parent.Parent;
				}

				return s;
			}
		}

		public string DisplayName
		{
			get { return name_; }
		}

		public DateTime DateCreated
		{
			get { return FMS.FileCreationTime(VirtualPath); }
		}

		public DateTime DateModified
		{
			get { return FMS.FileLastWriteTime(VirtualPath); }
		}

		public bool CanPin
		{
			get { return false; }
		}

		public bool Virtual
		{
			get { return false; }
		}

		public bool IsFlattened
		{
			get { return false; }
		}

		public IPackage ParentPackage
		{
			get
			{
				IFilesystemObject o = this;

				while (o != null)
				{
					if (o is IPackage)
						return o as IPackage;

					o = o.Parent;
				}

				return null;
			}
		}

		public Icon Icon
		{
			get { return Icons.File(MakeRealPath()); }
		}

		public string MakeRealPath()
		{
			string s = Name;

			if (Parent != null)
				s = Parent.MakeRealPath() + s;

			return s;
		}
	}


	class Package : BasicFilesystemContainer, IPackage
	{
		private readonly ShortCut sc_;

		public Package(Filesystem fs, IFilesystemContainer parent, ShortCut sc)
			: base(fs, parent, sc.package)
		{
			sc_ = sc;
		}

		public ShortCut ShortCut
		{
			get { return sc_; }
		}

		public override string DisplayName
		{
			get { return sc_.package; }
		}

		public override DateTime DateCreated
		{
			get { return FMS.FileCreationTime(sc_.path); }
		}

		public override DateTime DateModified
		{
			get { return FMS.FileLastWriteTime(sc_.path); }
		}

		public override Icon Icon
		{
			get { return Icons.Package; }
		}

		public override bool CanPin
		{
			get { return true; }
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override bool IsFlattened
		{
			get { return false; }
		}

		public override string MakeRealPath()
		{
			return sc_.path + "/";
		}

		protected override List<IFilesystemContainer> GetSubDirectoriesImpl(Filter filter)
		{
			return DoGetSubDirectories(c_, filter);
		}

		protected override List<IFilesystemObject> GetFilesImpl(Caches c, Filter filter)
		{
			return DoGetFiles(c, filter);
		}

		protected override void GetFilesRecursiveImpl(Filter filter)
		{
			DoGetFilesRecursive(rc_.files.entries);
		}
	}


	class NullDirectory : IDirectory
	{
		public string Name { get { return ""; } }
		public string VirtualPath { get { return ""; } }
		public string DisplayName { get { return ""; } }
		public DateTime DateCreated { get { return DateTime.MaxValue; } }
		public DateTime DateModified { get { return DateTime.MaxValue; } }
		public bool CanPin { get { return false; } }
		public bool Virtual { get { return true; } }
		public bool IsFlattened { get { return false; } }
		public IPackage ParentPackage { get { return null; } }

		public IFilesystemContainer Parent
		{
			get { return null; }
		}

		public List<IFilesystemObject> GetFiles(Filter filter)
		{
			return new List<IFilesystemObject>();
		}

		public List<IFilesystemObject> GetFilesRecursive(Filter filter)
		{
			return new List<IFilesystemObject>();
		}

		public Icon Icon
		{
			get { return Icons.Null; }
		}

		public List<IFilesystemContainer> GetSubDirectories(Filter filter)
		{
			return new List<IFilesystemContainer>();
		}

		public bool HasSubDirectories(Filter filter)
		{
			return false;
		}

		public string MakeRealPath()
		{
			return "";
		}
	}
}
