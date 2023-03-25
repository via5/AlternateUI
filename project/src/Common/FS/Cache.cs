using System.Collections.Generic;

namespace AUI.FS
{
	class CachedListing<T> where T : class, IFilesystemObject
	{
		private int cacheToken_ = -1;
		private bool showHiddenFolders_ = false;
		private bool showHiddenFiles_ = false;
		private bool latestPackagesOnly_ = false;
		private bool mergePackages_ = false;
		private string packagesRoot_ = null;
		private Listing<T> listing_ = null;

		public bool ShowHiddenFolders
		{
			get { return showHiddenFolders_; }
		}

		public bool ShowHiddenFiles
		{
			get { return showHiddenFiles_; }
		}

		public bool LatestPackagesOnly
		{
			get { return latestPackagesOnly_; }
		}

		public bool MergePackages
		{
			get { return mergePackages_; }
		}

		public string PackagesRoot
		{
			get { return packagesRoot_; }
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
			latestPackagesOnly_ = cx.LatestPackagesOnly;
			mergePackages_ = cx.MergePackages;
			packagesRoot_ = cx.PackagesRoot;

			if (listing_ == null)
				listing_ = new Listing<T>();

			listing_.SetRaw(list);
		}
	}


	class Cache
	{
		private CachedListing<IFilesystemContainer> localDirs_ = null;
		private CachedListing<IFilesystemObject> localFiles_ = null;
		private CachedListing<IFilesystemObject> recursiveFiles_ = null;
		private Dictionary<string, IFilesystemObject> resolved_ = null;

		public void Clear()
		{
			localDirs_?.Clear();
			localFiles_?.Clear();
			recursiveFiles_?.Clear();
			resolved_?.Clear();
		}


		public bool Resolve(string path, out IFilesystemObject o)
		{
			if (resolved_ == null)
			{
				o = null;
				return false;
			}

			return resolved_.TryGetValue(path, out o);
		}

		public void AddResolve(string path, IFilesystemObject o)
		{
			if (resolved_ == null)
				resolved_ = new Dictionary<string, IFilesystemObject>();

			IFilesystemObject oo;
			if (resolved_.TryGetValue(path, out oo))
				AlternateUI.Assert(oo == o);
			else
				resolved_.Add(path, o);
		}


		public bool StaleLocalDirectoriesCache(Filesystem fs, Context cx)
		{
			if (localDirs_?.Stale(fs) ?? true)
				return true;

			if (localDirs_.ShowHiddenFolders != cx.ShowHiddenFolders)
				return true;

			if (localDirs_.MergePackages != cx.MergePackages)
				return true;

			if (localDirs_.PackagesRoot != cx.PackagesRoot)
				return true;

			if (localDirs_.LatestPackagesOnly != cx.LatestPackagesOnly)
				return true;

			return false;
		}

		public void SetLocalDirectoriesCache(
			Filesystem fs, Context cx, List<IFilesystemContainer> list)
		{
			if (localDirs_ == null)
				localDirs_ = new CachedListing<IFilesystemContainer>();

			localDirs_.SetRaw(fs, cx, list);
			resolved_?.Clear();
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

			if (localFiles_.MergePackages != cx.MergePackages)
				return true;

			return false;
		}

		public void SetLocalFilesCache(
			Filesystem fs, Context cx, List<IFilesystemObject> list)
		{
			if (localFiles_ == null)
				localFiles_ = new CachedListing<IFilesystemObject>();

			localFiles_.SetRaw(fs, cx, list);
			resolved_?.Clear();
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

			if (recursiveFiles_.MergePackages != cx.MergePackages)
				return true;

			if (recursiveFiles_.PackagesRoot != cx.PackagesRoot)
				return true;

			return false;
		}

		public void SetRecursiveFilesCache(Filesystem fs, Context cx)
		{
			if (recursiveFiles_ == null)
				recursiveFiles_ = new CachedListing<IFilesystemObject>();

			recursiveFiles_.SetRaw(fs, cx, null);
			resolved_?.Clear();
		}

		public Listing<IFilesystemObject> GetRecursiveFilesCache()
		{
			if (recursiveFiles_ == null)
				recursiveFiles_ = new CachedListing<IFilesystemObject>();

			return recursiveFiles_.Listing;
		}
	}
}
