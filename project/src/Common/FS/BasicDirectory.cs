using System;
using System.Collections.Generic;

namespace AUI.FS
{
	public abstract class BasicFilesystemContainer
		: BasicFilesystemObject, IFilesystemContainer
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

		public override bool IsFile
		{
			get { return false; }
		}

		public override void ClearCache()
		{
			base.ClearCache();
			cache_?.Clear(false);
		}

		public void ClearCacheKeepResolve()
		{
			cache_?.Clear(true);
		}

		public virtual bool AlreadySorted
		{
			get { return false; }
		}


		public bool HasDirectories(Context cx)
		{
			if (!StaleLocalDirectoriesCache(cx))
			{
				var dirs = cache_.GetLocalDirectories().Last;
				return (dirs != null && dirs.Count > 0);
			}

			return HasDirectoriesInternal(cx);
		}


		private bool StaleLocalDirectoriesCache(Context cx)
		{
			if (cache_ == null)
				return true;

			return cache_.StaleLocalDirectoriesCache(fs_, cx);
		}

		private void SetLocalDirectoriesCache(
			Context cx,
			List<IFilesystemContainer> allDirs,
			List<IFilesystemContainer> filteredDirs)
		{
			Instrumentation.Start(I.SetLocalDirectoriesCache);
			{
				if (cache_ == null)
					cache_ = new Cache();

				cache_.SetLocalDirectoriesCache(fs_, cx, allDirs, filteredDirs);
			}
			Instrumentation.End();
		}

		private Listing<IFilesystemContainer> GetLocalDirectoriesCache()
		{
			if (cache_ == null)
				cache_ = new Cache();

			return cache_.GetLocalDirectoriesCache();
		}


		private bool StaleLocalFilesCache(Context cx)
		{
			if (cache_ == null)
				return true;

			return cache_.StaleLocalFilesCache(fs_, cx);
		}

		private void SetLocalFilesCache(
			Context cx,
			List<IFilesystemObject> allFiles,
			List<IFilesystemObject> filteredFiles)
		{
			if (cache_ == null)
				cache_ = new Cache();

			cache_.SetLocalFilesCache(fs_, cx, allFiles, filteredFiles);
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
				List<IFilesystemContainer> allDirs = null;
				List<IFilesystemContainer> filteredDirs = null;

				GetDirectoriesInternal(cx, out allDirs, out filteredDirs);
				SetLocalDirectoriesCache(cx, allDirs, filteredDirs);
			}

			// dirs are not filtered for now
			//Filter(cx, cache_.GetLocalDirectories());

			Instrumentation.Start(I.SortDirectories);
			{
				SortInternal(cx, cache_.GetLocalDirectories());
			}
			Instrumentation.End();

			Instrumentation.Start(I.UpdateLookup);
			{
				cache_.GetLocalDirectories().UpdateLookup();
			}
			Instrumentation.End();

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
					List<IFilesystemObject> allFiles;
					List<IFilesystemObject> filteredFiles;

					GetFilesInternal(cx, out allFiles, out filteredFiles);
					SetLocalFilesCache(cx, allFiles, filteredFiles);
				}

				listing = GetLocalFilesCache();
			}

			Instrumentation.Start(I.GetFilesFilter);
			{
				Filter(cx, listing);
			}
			Instrumentation.End();

			return listing.Last;
		}

		public void GetFilesRecursiveInternal(
			Context cx, Listing<IFilesystemObject> listing)
		{
			if (StaleLocalFilesCache(cx))
			{
				List<IFilesystemObject> allFiles;
				List<IFilesystemObject> filteredFiles;

				GetFilesInternal(cx, out allFiles, out filteredFiles);
				SetLocalFilesCache(cx, allFiles, filteredFiles);

				listing.AddRaw(filteredFiles ?? allFiles);
			}
			else
			{
				listing.AddRaw(GetLocalFilesCache().Raw);
			}

			if (StaleLocalDirectoriesCache(cx))
			{
				// don't use context, it'll filter with extensions, etc.

				var cx2 = new Context(
					"", null, cx.PackagesRoot,
					Context.NoSort, Context.NoSortDirection, cx.Flags, "", "",
					cx.Whitelist);

				List<IFilesystemContainer> allDirs = null;
				List<IFilesystemContainer> filteredDirs = null;

				GetDirectoriesInternal(cx, out allDirs, out filteredDirs);
				SetLocalDirectoriesCache(cx, allDirs, filteredDirs);

				cache_.GetLocalDirectories().UpdateLookup();
			}

			DoGetFilesRecursive(cx, listing, GetLocalDirectoriesCache().Raw);
		}

		public override IFilesystemObject Resolve(
			Context cx, string path, int flags = Filesystem.ResolveDefault)
		{
			IFilesystemObject o;

			Instrumentation.Start(I.Resolve);
			{
				o = ResolveImpl(cx, path, flags);
			}
			Instrumentation.End();

			return o;
		}

		private IFilesystemObject ResolveImpl(
			Context cx, string path, int flags = Filesystem.ResolveDefault)
		{
			try
			{
				if (path == null)
				{
					Log.Error($"{this} ResolveImpl path is null");
					return null;
				}

				if (path == "")
					return this;

				if (Bits.IsSet(flags, Filesystem.ResolveCacheOnly))
				{
					if (cache_ == null)
						return null;
				}

				if (!Bits.IsSet(flags, Filesystem.ResolveNoCache))
				{
					if (cache_ != null)
					{
						IFilesystemObject o;
						if (cache_.Resolve(path, out o))
							return o;
					}
				}

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

				if (!Bits.IsSet(flags, Filesystem.ResolveNoCache))
				{
					if (cache_ == null)
						cache_ = new Cache();

					cache_.AddResolve(path, r.o);
				}

				return r.o;
			}
			catch (Exception e)
			{
				Log.Error($"exception while resolving '{path}':");
				Log.Error(e.ToString());

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

		public ResolveResult ResolveInternal2(
			Context cx, PathComponents cs, int flags, ResolveDebug debug)
		{
			ResolveResult rr;


			Instrumentation.Start(I.ResolveInternalInDirectories);
			{
				rr = ResolveInternalInDirectories(cx, cs, flags, debug);
			}
			Instrumentation.End();

			if (rr.o != null)
				return rr;


			Instrumentation.Start(I.ResolveInternalInFiles);
			{
				rr = ResolveInternalInFiles(cx, cs, flags, debug);
			}
			Instrumentation.End();

			if (rr.o != null)
				return rr;


			return ResolveResult.NotFound();
		}

		private ResolveResult ResolveInternalInDirectories(
			Context cx, PathComponents cs, int flags, ResolveDebug debug)
		{
			if (!Bits.IsSet(flags, Filesystem.ResolveCacheOnly))
				GetDirectories(cx);

			var dirs = cache_.GetLocalDirectories().Lookup;

			if (dirs.Count == 0)
			{
				if (debug.Enabled)
					debug.Info(this, $"resolveinternal, has no dirs");
			}

			IFilesystemContainer d;
			if (dirs.TryGetValue(cs.Current, out d))
			{
				if (debug.Enabled)
					debug.Info(this, $"resolveinternal, found '{cs.Current}' in lookup, {d}, cs={cs}");

				if (cs.Last)
				{
					if (debug.Enabled)
						debug.Info(this, $"resolveinternal, is this final, cs={cs} <<<<<");

					return ResolveResult.Found(d);
				}
				else
				{
					return d.ResolveInternal(cx, cs, flags, debug.Inc());
				}
			}

			if (debug.Enabled)
				debug.Info(this, $"resolveinternal, not a directory, cs={cs}");

			return ResolveResult.NotFound();
		}

		public ResolveResult ResolveInternalInFiles(
			Context cx, PathComponents cs, int flags, ResolveDebug debug)
		{
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

				List<IFilesystemObject> files;

				if (Bits.IsSet(flags, Filesystem.ResolveCacheOnly))
					files = GetLocalFilesCache().Last;
				else
					files = GetFiles(cx);

				foreach (var f in files)
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

		protected abstract List<IFilesystemContainer> DoGetDirectories(Context cx);

		protected virtual bool DoHasDirectories(Context cx)
		{
			bool b = false;

			Instrumentation.Start(I.BasicDoHasDirectories);
			{
				var path = MakeRealPath();

				if (!string.IsNullOrEmpty(path))
				{
					var dirs = SysWrappers.GetDirectories(this, path);
					b = (dirs != null && dirs.Length > 0);
				}
			}
			Instrumentation.End();

			return b;
		}

		protected virtual List<IFilesystemObject> DoGetFiles(Context cx)
		{
			List<IFilesystemObject> list = null;

			Instrumentation.Start(I.BasicDoGetFiles);
			{
				var path = MakeRealPath();

				if (!string.IsNullOrEmpty(path))
				{
					var files = SysWrappers.GetFiles(this, path);
					list = new List<IFilesystemObject>(files.Length);

					foreach (var filePath in files)
						list.Add(new FSFile(fs_, this, Path.Filename(filePath)));
				}
			}
			Instrumentation.End();

			return list;
		}

		protected virtual void DoGetFilesRecursive(
			Context cx, Listing<IFilesystemObject> listing,
			List<IFilesystemContainer> dirs)
		{
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


		private bool IncludeDirectory(Context cx, IFilesystemContainer o)
		{
			if (cx.ShowHiddenFolders)
				return true;


			if (!cx.ShowHiddenFolders)
			{
				if (o.Name.StartsWith("."))
					return false;
			}


			string rvp = null;

			Instrumentation.Start(I.IncludeDirectoryGetRVP);
			{
				if (cx.Whitelist != null)
					rvp = o.RelativeVirtualPath;
			}
			Instrumentation.End();


			bool wm;

			Instrumentation.Start(I.IncludeDirectoryWhitelistMatches);
			{
				wm = cx.WhitelistMatches(rvp);
			}
			Instrumentation.End();

			if (wm)
				return true;

			if (fs_.HasPinnedParent(o))
				return true;

			return DoIncludeDirectory(cx, o);
		}

		protected virtual bool DoIncludeDirectory(Context cx, IFilesystemContainer o)
		{
			return false;
		}


		private bool IncludeFile(Context cx, IFilesystemObject o)
		{
			if (!string.IsNullOrEmpty(cx.RemovePrefix))
			{
				if (!o.Name.StartsWith(cx.RemovePrefix))
					return false;
			}

			if (!cx.ShowHiddenFiles)
			{
				if (o.Name.StartsWith("."))
					return false;
			}

			return DoIncludeFile(cx, o);
		}

		protected virtual bool DoIncludeFile(Context cx, IFilesystemObject o)
		{
			return true;
		}


		private bool HasDirectoriesInternal(Context cx)
		{
			bool b;

			Instrumentation.Start(I.CallingDoHasDirectories);
			{
				b = DoHasDirectories(cx);
			}
			Instrumentation.End();

			return b;
		}

		private void GetDirectoriesInternal(
			Context cx,
			out List<IFilesystemContainer> allDirs,
			out List<IFilesystemContainer> filteredDirs)
		{
			allDirs = null;
			filteredDirs = null;

			Instrumentation.Start(I.CallingDoGetDirectories);
			{
				allDirs = DoGetDirectories(cx);
			}
			Instrumentation.End();

			Instrumentation.Start(I.CheckDirectories);
			{
				if (allDirs != null)
				{
					for (int i = 0; i < allDirs.Count; ++i)
					{
						var d = allDirs[i];

						if (filteredDirs == null)
						{
							if (!IncludeDirectory(cx, d))
							{
								filteredDirs = new List<IFilesystemContainer>();

								for (int j = 0; j < i; ++j)
									filteredDirs.Add(allDirs[j]);
							}
						}
						else
						{
							if (IncludeDirectory(cx, d))
								filteredDirs.Add(d);
						}
					}
				}
			}
			Instrumentation.End();
		}

		private void GetFilesInternal(
			Context cx,
			out List<IFilesystemObject> allFiles,
			out List<IFilesystemObject> filteredFiles)
		{
			allFiles = null;
			filteredFiles = null;

			Instrumentation.Start(I.CallingDoGetFiles);
			{
				// unset recursive, this is just for this container
				bool oldRecursive = cx.Recursive;
				cx.Recursive = false;
				allFiles = DoGetFiles(cx);
				cx.Recursive = oldRecursive;
			}
			Instrumentation.End();

			Instrumentation.Start(I.CheckFiles);
			{
				if (allFiles != null)
				{
					for (int i = 0; i < allFiles.Count; ++i)
					{
						var f = allFiles[i];

						if (filteredFiles == null)
						{
							if (!IncludeFile(cx, f))
							{
								filteredFiles = new List<IFilesystemObject>();

								for (int j = 0; j < i; ++j)
									filteredFiles.Add(allFiles[j]);
							}
						}
						else
						{
							if (IncludeFile(cx, f))
								filteredFiles.Add(f);
						}
					}
				}
			}
			Instrumentation.End();
		}

		private void Filter<EntryType>(Context cx, Listing<EntryType> listing)
			where EntryType : class, IFilesystemObject
		{
			Instrumentation.Start(I.GetFilesFilterExtensions);
			{
				FilterExtensions(cx, listing);
			}
			Instrumentation.End();

			Instrumentation.Start(I.GetFilesFilterSearch);
			{
				FilterSearch(cx, listing);
			}
			Instrumentation.End();

			Instrumentation.Start(I.GetFilesFilterSort);
			{
				SortInternal(cx, listing);
			}
			Instrumentation.End();
		}

		private void FilterExtensions<EntryType>(Context cx, Listing<EntryType> listing)
			where EntryType : class, IFilesystemObject
		{
			if (listing.ExtensionsStale(cx))
			{
				if (cx.ExtensionsString == "" || cx.ExtensionsString == "*.*")
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
							if (cx.ExtensionMatches(f))
								perExtension.Add(f);
						}
					}

					listing.SetExtensions(cx, perExtension);
				}
			}
		}

		private void FilterSearch<EntryType>(Context cx, Listing<EntryType> listing)
			where EntryType : class, IFilesystemObject
		{
			if (listing.SearchStale(cx))
			{
				if (cx.Search.Empty)
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
							if (cx.SearchMatches(f))
								searched.Add(f);
						}
					}

					listing.SetSearched(cx, searched);
				}
			}

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

							Instrumentation.Start(I.GetFilesFilterSortCopy);
							{
								sorted = new List<EntryType>(parentList);
							}
							Instrumentation.End();


							Instrumentation.Start(I.GetFilesFilterSortSort);
							{
								cx.SortList(sorted);
							}
							Instrumentation.End();
						}

						listing.SetSorted(cx, sorted);
					}
				}
			}
		}
	}
}
