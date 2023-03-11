using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;
using System.Globalization;

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
		public abstract VUI.Icon Icon { get; }
		public abstract bool CanPin { get; }
		public abstract bool Virtual { get; }
		public abstract bool ChildrenVirtual { get; }
		public abstract bool IsFlattened { get; }

		public virtual bool IsRedundant
		{
			get { return false; }
		}


		public virtual string Tooltip
		{
			get
			{
				string tt =
					$"{base.ToString()}\n" +
					$"Virtual path: {VirtualPath}" +
					$"\nReal path: {(Virtual ? "(virtual)" : MakeRealPath())}";

				var p = ParentPackage;

				if (p != null)
					tt += $"\nPackage: {p.DisplayName}";

				tt += $"\nCreated: {FormatDT(DateCreated)}";
				tt += $"\nLast modified: {FormatDT(DateModified)}";

				return tt;
			}
		}

		private string FormatDT(DateTime dt)
		{
			return dt.ToString(CultureInfo.CurrentCulture);
		}

		public virtual bool IsSameObject(IFilesystemObject o)
		{
			return fs_.IsSameObject(this, o);
		}

		public virtual IFilesystemObject Resolve(
			Context cx, string path, int flags = Filesystem.ResolveDefault)
		{
			if (path == Name)
				return this;

			return null;
		}

		public abstract string MakeRealPath();

		public virtual void ClearCache()
		{
			// no-op
		}

		protected virtual string GetDisplayName()
		{
			return Name;
		}

		protected virtual void DisplayNameChanged()
		{
			// no-op
		}
	}


	class CachedListing<T> where T : class, IFilesystemObject
	{
		private int cacheToken_ = -1;
		private bool showHiddenFolders_ = false;
		private bool showHiddenFiles_ = false;
		private Listing<T> listing_ = null;

		public bool ShowHiddenFolders
		{
			get { return showHiddenFolders_; }
		}

		public bool ShowHiddenFiles
		{
			get { return showHiddenFiles_; }
		}

		public Listing<T> Listing
		{
			get { return listing_; }
		}

		public void Clear()
		{
			cacheToken_ = -1;
		}

		public bool Stale(Filesystem fs)
		{
			if (cacheToken_ != fs.CacheToken)
				return true;

			if (listing_?.Raw == null)
				return true;

			return false;
		}

		public void SetRaw(Filesystem fs, Context cx, List<T> list)
		{
			cacheToken_ = fs.CacheToken;
			showHiddenFolders_ = cx.ShowHiddenFolders;
			showHiddenFiles_ = cx.ShowHiddenFiles;

			if (listing_ == null)
				listing_ = new Listing<T>();

			listing_.SetRaw(list);
		}
	}


	class Cache
	{
		private CachedListing<IFilesystemContainer> localDirs_;
		private CachedListing<IFilesystemObject> localFiles_;
		private CachedListing<IFilesystemObject> recursiveFiles_;

		public void Clear()
		{
			localDirs_?.Clear();
			localFiles_?.Clear();
			recursiveFiles_?.Clear();
		}

		public bool StaleLocalDirectoriesCache(Filesystem fs, Context cx)
		{
			if (localDirs_?.Stale(fs) ?? true)
				return true;

			if (localDirs_.ShowHiddenFolders != cx.ShowHiddenFolders)
				return true;

			return false;
		}

		public void SetLocalDirectoriesCache(
			Filesystem fs, Context cx, List<IFilesystemContainer> list)
		{
			if (localDirs_ == null)
				localDirs_ = new CachedListing<IFilesystemContainer>();

			localDirs_.SetRaw(fs, cx, list);
		}

		public Listing<IFilesystemContainer> GetLocalDirectories()
		{
			return localDirs_.Listing;
		}

		public Listing<IFilesystemContainer> GetLocalDirectoriesCache()
		{
			if (localDirs_ == null)
				localDirs_ = new CachedListing<IFilesystemContainer>();

			return localDirs_.Listing;
		}


		public bool StaleLocalFilesCache(Filesystem fs, Context cx)
		{
			if (localFiles_?.Stale(fs) ?? true)
				return true;

			if (localFiles_.ShowHiddenFolders != cx.ShowHiddenFolders)
				return true;

			if (localFiles_.ShowHiddenFiles != cx.ShowHiddenFiles)
				return true;

			return false;
		}

		public void SetLocalFilesCache(
			Filesystem fs, Context cx, List<IFilesystemObject> list)
		{
			if (localFiles_ == null)
				localFiles_ = new CachedListing<IFilesystemObject>();

			localFiles_.SetRaw(fs, cx, list);
		}

		public Listing<IFilesystemObject> GetLocalFilesCache()
		{
			if (localFiles_ == null)
				localFiles_ = new CachedListing<IFilesystemObject>();

			return localFiles_.Listing;
		}


		public bool StaleRecursiveFilesCache(Filesystem fs, Context cx)
		{
			if (recursiveFiles_?.Stale(fs) ?? true)
				return true;

			if (recursiveFiles_.ShowHiddenFolders != cx.ShowHiddenFolders)
				return true;

			if (recursiveFiles_.ShowHiddenFiles != cx.ShowHiddenFiles)
				return true;

			return false;
		}

		public void SetRecursiveFilesCache(Filesystem fs, Context cx)
		{
			if (recursiveFiles_ == null)
				recursiveFiles_ = new CachedListing<IFilesystemObject>();

			recursiveFiles_.SetRaw(fs, cx, null);
		}

		public Listing<IFilesystemObject> GetRecursiveFilesCache()
		{
			if (recursiveFiles_ == null)
				recursiveFiles_ = new CachedListing<IFilesystemObject>();

			return recursiveFiles_.Listing;
		}
	}


	abstract class BasicFilesystemContainer : BasicFilesystemObject, IFilesystemContainer
	{
		private readonly string name_;
		private Cache cache_ = null;

		public BasicFilesystemContainer(Filesystem fs, IFilesystemContainer parent, string name)
			: base(fs, parent)
		{
			name_ = name;
		}

		public override string Name
		{
			get { return name_; }
		}

		public override void ClearCache()
		{
			cache_?.Clear();
		}

		public virtual bool AlreadySorted
		{
			get { return false; }
		}


		public bool HasDirectories(Context cx)
		{
			return (GetDirectories(cx).Count > 0);
		}


		private bool StaleLocalDirectoriesCache(Context cx)
		{
			return (cache_?.StaleLocalDirectoriesCache(fs_, cx) ?? true);
		}

		private void SetLocalDirectoriesCache(
			Context cx, List<IFilesystemContainer> list)
		{
			if (cache_ == null)
				cache_ = new Cache();

			cache_.SetLocalDirectoriesCache(fs_, cx, list);
		}

		private Listing<IFilesystemContainer> GetLocalDirectoriesCache()
		{
			if (cache_ == null)
				cache_ = new Cache();

			return cache_.GetLocalDirectoriesCache();
		}


		private bool StaleLocalFilesCache(Context cx)
		{
			return (cache_?.StaleLocalFilesCache(fs_, cx) ?? true);
		}

		private void SetLocalFilesCache(Context cx, List<IFilesystemObject> list)
		{
			if (cache_ == null)
				cache_ = new Cache();

			cache_.SetLocalFilesCache(fs_, cx, list);
		}

		private Listing<IFilesystemObject> GetLocalFilesCache()
		{
			if (cache_ == null)
				cache_ = new Cache();

			return cache_.GetLocalFilesCache();
		}


		private bool StaleRecursiveFilesCache(Context cx)
		{
			return (cache_?.StaleRecursiveFilesCache(fs_, cx) ?? true);
		}

		private void SetRecursiveFilesCache(Context cx)
		{
			if (cache_ == null)
				cache_ = new Cache();

			cache_.SetRecursiveFilesCache(fs_, cx);
		}

		private Listing<IFilesystemObject> GetRecursiveFilesCache()
		{
			if (cache_ == null)
				cache_ = new Cache();

			return cache_.GetRecursiveFilesCache();
		}


		public List<IFilesystemContainer> GetDirectories(Context cx)
		{
			if (StaleLocalDirectoriesCache(cx))
			{
				var dirs = GetDirectoriesInternal(cx);
				SetLocalDirectoriesCache(cx, dirs);
			}

			Filter(cx, cache_.GetLocalDirectories());

			return cache_.GetLocalDirectories().Last;
		}

		public List<IFilesystemObject> GetFiles(Context cx)
		{
			Listing<IFilesystemObject> listing;

			if (cx.Recursive || IsFlattened)
			{
				if (StaleRecursiveFilesCache(cx))
				{
					SetRecursiveFilesCache(cx);
					GetFilesRecursiveInternal(cx, GetRecursiveFilesCache());
				}

				listing = GetRecursiveFilesCache();
			}
			else
			{
				if (StaleLocalFilesCache(cx))
				{
					var files = GetFilesInternal(cx);
					SetLocalFilesCache(cx, files);
				}

				listing = GetLocalFilesCache();
			}

			Filter(cx, listing);

			return listing.Last;
		}

		public void GetFilesRecursiveInternal(
			Context cx, Listing<IFilesystemObject> listing)
		{
			if (StaleLocalFilesCache(cx))
			{
				var list = GetFilesInternal(cx);
				SetLocalFilesCache(cx, list);
				listing.AddRaw(list);
			}
			else
			{
				listing.AddRaw(GetLocalFilesCache().Raw);
			}

			List<IFilesystemContainer> dirs;

			if (StaleLocalDirectoriesCache(cx))
			{
				dirs = GetDirectoriesInternal(cx);
				SetLocalDirectoriesCache(cx, dirs);
			}
			else
			{
				dirs = GetLocalDirectoriesCache().Raw;
			}

			if (dirs != null)
			{
				foreach (var sd in dirs)
				{
					if (sd.IsFlattened || sd.IsRedundant)
						continue;

					sd.GetFilesRecursiveInternal(cx, listing);
				}
			}
		}

		public override IFilesystemObject Resolve(
			Context cx, string path, int flags = Filesystem.ResolveDefault)
		{
			try
			{
				if (path == null)
					return null;

				if (path == "")
					return this;

				var cs = new PathComponents(path);
				ResolveDebug debug = ResolveDebug.Null;

				if (Bits.IsSet(flags, Filesystem.ResolveDebug))
					debug = new ResolveDebug(0);

				if (debug.Enabled)
				{
					debug.Info(null, "");
					debug.Info(this, $"resolve cs={cs}");
				}

				var r = ResolveInternal(cx, cs, flags, debug);

				if (debug.Enabled)
					debug.Info(null, "");

				if (r.partial)
					return null;
				else
					return r.o;
			}
			catch (Exception e)
			{
				AlternateUI.Instance.Log.Error(
					$"exception while resolving '{path}':");

				AlternateUI.Instance.Log.Error(e.ToString());

				return null;
			}
		}

		public virtual ResolveResult ResolveInternal(
			Context cx, PathComponents cs, int flags, ResolveDebug debug)
		{
			if (cs.Done || cs.Current != Name)
			{
				if (debug.Enabled)
				{
					debug.Info(this,
						$"resolveinternal failed, " +
						$"name='{Name}' cs={cs} flags={flags} ");
				}

				return ResolveResult.NotFound();
			}

			if (cs.Last)
			{
				if (debug.Enabled)
					debug.Info(this, $"resolveinternal, is this final, cs={cs} <<<<<");

				return ResolveResult.Found(this);
			}

			if (debug.Enabled)
				debug.Info(this, $"resolveinternal, is this, cs={cs}");

			return ResolveInternal2(cx, cs.NextCopy(), flags, debug);
		}

		public virtual ResolveResult ResolveInternal2(
			Context cx, PathComponents cs, int flags, ResolveDebug debug)
		{
			if (GetDirectories(cx).Count == 0)
			{
				if (debug.Enabled)
					debug.Info(this, $"resolveinternal, has no dirs");
			}

			foreach (var d in GetDirectories(cx))
			{
				var r = d.ResolveInternal(cx, cs, flags, debug.Inc());

				if (r.o == null || r.partial)
				{
					if (debug.Enabled)
						debug.Info(this, $"resolveinternal, not dir {d}, cs={cs}");

					if (r.partial)
						return r;
				}
				else
				{
					if (debug.Enabled)
					{
						debug.Info(this,
							$"resolveinternal2, is dir {d}, cs={cs}");
					}

					return r;
				}
			}

			if (debug.Enabled)
				debug.Info(this, $"resolveinternal, not a directory, cs={cs}");

			if (!Bits.IsSet(flags, Filesystem.ResolveDirsOnly))
			{
				if (!cs.NextIsLast)
				{
					// cannot be a file

					if (debug.Enabled)
					{
						debug.Info(this,
							$"resolveinternal2 failed, cannot be a " +
							$"file, cs={cs}");
					}

					return ResolveResult.NotFound();
				}

				cs.Next();

				foreach (var f in GetFiles(cx))
				{
					if (f.Name == cs.Current)
					{
						return ResolveResult.Found(f);
					}
					else
					{
						if (debug.Enabled)
						{
							debug.Info(this,
								$"resolveinternal2 failed, not file {f}");
						}
					}
				}
			}

			return ResolveResult.NotFound();
		}


		protected virtual List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			var list = new List<IFilesystemContainer>();
			var path = MakeRealPath();

			if (!string.IsNullOrEmpty(path))
			{
				foreach (var dirPath in GetDirectoriesFromFMS(path))
				{
					var vd = new VirtualDirectory(fs_, this, Path.Filename(dirPath));
					vd.Add(new FSDirectory2(fs_, this, Path.Filename(dirPath)));
					list.Add(vd);
				}
			}

			return list;
		}

		protected virtual List<IFilesystemObject> DoGetFiles(Context cx)
		{
			var list = new List<IFilesystemObject>();
			var path = MakeRealPath();

			if (!string.IsNullOrEmpty(path))
			{
				foreach (var filePath in GetFilesFromFMS(path))
					list.Add(new FSFile(fs_, this, Path.Filename(filePath)));
			}

			return list;
		}

		protected virtual bool IncludeDirectory(Context cx, IFilesystemContainer o)
		{
			return true;
		}

		protected virtual bool IncludeFile(Context cx, IFilesystemObject o)
		{
			return true;
		}


		private List<IFilesystemContainer> GetDirectoriesInternal(Context cx)
		{
			var dirs = DoGetDirectories(cx);

			if (dirs != null)
			{
				List<IFilesystemContainer> checkedDirs = null;

				for (int i = 0; i < dirs.Count; ++i)
				{
					var d = dirs[i];

					if (checkedDirs == null)
					{
						if (!IncludeDirectory(cx, d))
						{
							checkedDirs = new List<IFilesystemContainer>();

							for (int j = 0; j < i; ++j)
								checkedDirs.Add(dirs[j]);
						}
					}
					else
					{
						if (IncludeDirectory(cx, d))
							checkedDirs.Add(d);
					}
				}

				if (checkedDirs != null)
					dirs = checkedDirs;
			}

			return dirs;
		}

		private List<IFilesystemObject> GetFilesInternal(Context cx)
		{
			var files = DoGetFiles(cx);

			if (files != null)
			{
				List<IFilesystemObject> checkedFiles = null;

				for (int i = 0; i < files.Count; ++i)
				{
					var f = files[i];

					if (checkedFiles == null)
					{
						if (!IncludeFile(cx, f))
						{
							checkedFiles = new List<IFilesystemObject>();

							for (int j = 0; j < i; ++j)
								checkedFiles.Add(files[j]);
						}
					}
					else
					{
						if (IncludeFile(cx, f))
							checkedFiles.Add(f);
					}
				}

				if (checkedFiles != null)
					files = checkedFiles;
			}

			return files;
		}

		private void Filter<EntryType>(Context cx, Listing<EntryType> listing)
			where EntryType : class, IFilesystemObject
		{
			if (listing.ExtensionsStale(cx))
			{
				if (cx.ExtensionsString == "")
				{
					listing.SetExtensions(cx, null);
				}
				else
				{
					List<EntryType> perExtension = null;

					if (listing.Raw != null)
					{
						perExtension = new List<EntryType>();

						foreach (var f in listing.Raw)
						{
							if (cx.ExtensionMatches(f.DisplayName))
								perExtension.Add(f);
						}
					}

					listing.SetExtensions(cx, perExtension);
				}
			}

			if (listing.SearchStale(cx))
			{
				if (cx.Search == "")
				{
					listing.SetSearched(cx, null);
				}
				else
				{
					List<EntryType> searched = null;

					if (listing.Raw != null)
					{
						searched = new List<EntryType>();

						foreach (var f in (listing.PerExtension ?? listing.Raw))
						{
							if (cx.SearchMatches(f.DisplayName))
								searched.Add(f);
						}
					}

					listing.SetSearched(cx, searched);
				}
			}

			SortInternal(cx, listing);
		}

		private void SortInternal<EntryType>(Context cx, Listing<EntryType> listing)
			where EntryType : class, IFilesystemObject
		{
			if (!AlreadySorted)
			{
				if (listing.SortStale(cx))
				{
					if (cx.Sort == Context.NoSort)
					{
						listing.SetSorted(cx, null);
					}
					else
					{
						List<EntryType> sorted = null;

						if (listing.Raw != null)
						{
							var parentList = listing.Searched ?? listing.PerExtension ?? listing.Raw;

							sorted = new List<EntryType>(parentList);
							cx.SortList(sorted);
						}

						listing.SetSorted(cx, sorted);
					}
				}
			}
		}

		private string[] GetDirectoriesFromFMS(string path)
		{
			try
			{
				return FMS.GetDirectories(path);
			}
			catch (Exception e)
			{
				AlternateUI.Instance.Log.ErrorST($"{this}: bad directory '{path}': {e.Message}");
				return new string[0];
			}
		}

		private string[] GetFilesFromFMS(string path)
		{
			try
			{
				return FMS.GetFiles(path);
			}
			catch (Exception e)
			{
				AlternateUI.Instance.Log.ErrorST($"{this}: bad directory '{path}': {e.Message}");
				return new string[0];
			}
		}
	}
}
