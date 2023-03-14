﻿using System.Collections.Generic;

namespace AUI.FS
{
	class CachedListing<T> where T : class, IFilesystemObject
	{
		private int cacheToken_ = -1;
		private bool showHiddenFolders_ = false;
		private bool showHiddenFiles_ = false;
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
			packagesRoot_ = cx.PackagesRoot;

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

			if (localDirs_.PackagesRoot != cx.PackagesRoot)
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

			if (recursiveFiles_.PackagesRoot != cx.PackagesRoot)
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
}
