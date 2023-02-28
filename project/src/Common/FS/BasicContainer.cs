﻿using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;

namespace AUI.FS
{
	using FMS = FileManagerSecure;


	abstract class BasicFilesystemObject : IFilesystemObject
	{
		protected readonly Filesystem fs_;
		private readonly IFilesystemContainer parent_;
		private string displayName_ = null;

		public BasicFilesystemObject(
			Filesystem fs, IFilesystemContainer parent,
			string displayName = null)
		{
			fs_ = fs;
			parent_ = parent;
			displayName_ = displayName;
		}

		public IFilesystemContainer Parent
		{
			get { return parent_; }
		}

		public string DisplayName
		{
			get
			{
				return displayName_ ?? GetDisplayName();
			}

			set
			{
				if (displayName_ != value)
				{
					displayName_ = value;
					DisplayNameChanged();
				}
			}
		}

		public bool HasCustomDisplayName
		{
			get { return (displayName_ != null); }
		}

		public virtual string VirtualPath
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

		public virtual IPackage ParentPackage
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

		public abstract string Name { get; }
		public abstract DateTime DateCreated { get; }
		public abstract DateTime DateModified { get; }
		public abstract Icon Icon { get; }
		public abstract bool CanPin { get; }
		public abstract bool Virtual { get; }
		public abstract bool IsFlattened { get; }


		public virtual bool IsSameObject(IFilesystemObject o)
		{
			return fs_.IsSameObject(this, o);
		}

		public abstract string MakeRealPath();

		protected virtual string GetDisplayName()
		{
			return Name;
		}

		protected virtual void DisplayNameChanged()
		{
			// no-op
		}
	}


	abstract class BasicFilesystemContainer : BasicFilesystemObject, IFilesystemContainer
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

			public void Clear()
			{
				entries = null;
				perExtension = null;
				searched = null;
				sorted = null;

				currentExtensions = "";
				currentSearch = "";
				currentSort = Filter.NoSort;
				currentSortDir = Filter.NoSortDirection;
			}
		}

		protected class Caches
		{
			private Cache<IFilesystemObject> files_ = new Cache<IFilesystemObject>();
			private Cache<IFilesystemContainer> dirs_ = new Cache<IFilesystemContainer>();

			public Cache<IFilesystemObject> Files
			{
				get
				{
					if (files_ == null)
						files_ = new Cache<IFilesystemObject>();

					return files_;
				}
			}

			public Cache<IFilesystemContainer> Directories
			{
				get
				{
					if (dirs_ == null)
						dirs_ = new Cache<IFilesystemContainer>();

					return dirs_;
				}
			}

			public void Clear()
			{
				files_?.Clear();
				dirs_?.Clear();
			}
		}


		private readonly string name_;
		protected Caches c_ = new Caches();
		protected Caches rc_ = new Caches();


		public BasicFilesystemContainer(Filesystem fs, IFilesystemContainer parent, string name)
			: base(fs, parent)
		{
			name_ = name;
		}

		public override string Name
		{
			get { return name_; }
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
			if (rc_.Files.entries == null)
			{
				rc_.Files.entries = new List<IFilesystemObject>();
				GetFilesRecursiveImpl(filter);
			}

			return FilterCaches(rc_.Files, filter);
		}

		public void GetFilesRecursiveUnfiltered(List<IFilesystemObject> list)
		{
			DoGetFilesRecursive(list);
		}

		protected abstract void GetFilesRecursiveImpl(Filter filter);

		public void ClearCaches()
		{
			c_.Clear();
			rc_.Clear();
		}


		protected List<IFilesystemContainer> DoGetSubDirectories(Caches c, Filter filter)
		{
			if (c.Directories.entries == null)
			{
				c.Directories.entries = new List<IFilesystemContainer>();

				foreach (var dirPath in GetDirectoriesFromFMS(MakeRealPath()))
				{
					var name = Path.Filename(dirPath);
					c.Directories.entries.Add(new FSDirectory(fs_, this, name));
				}
			}

			return FilterCaches(c.Directories, filter);
		}

		internal void DoGetFilesRecursive(List<IFilesystemObject> list)
		{
			list.AddRange(GetFiles(null));

			foreach (var sd in GetSubDirectories(null))
				sd.GetFilesRecursiveUnfiltered(list);
		}

		protected List<IFilesystemObject> DoGetFiles(Caches c, Filter filter)
		{
			if (c.Files.entries == null)
			{
				c.Files.entries = new List<IFilesystemObject>();

				foreach (var filePath in GetFilesFromFMS(MakeRealPath()))
				{
					var name = Path.Filename(filePath);

					if (filter == null || filter.Matches(name))
						c.Files.entries.Add(new FSFile(fs_, this, name));
				}
			}

			if (filter == null)
				return c.Files.entries;
			else
				return FilterCaches(c.Files, filter);
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
}
