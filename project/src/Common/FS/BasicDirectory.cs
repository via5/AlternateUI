﻿using System;
using System.Collections.Generic;

namespace AUI.FS
{
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
			return (cache_?.StaleLocalDirectoriesCache(fs_, cx) ?? true);
		}

		private void SetLocalDirectoriesCache(
			Context cx,
			List<IFilesystemContainer> allDirs,
			List<IFilesystemContainer> filteredDirs)
		{
			if (cache_ == null)
				cache_ = new Cache();

			cache_.SetLocalDirectoriesCache(fs_, cx, allDirs, filteredDirs);
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
				List<IFilesystemContainer> allDirs = null;
				List<IFilesystemContainer> filteredDirs = null;

				GetDirectoriesInternal(cx, out allDirs, out filteredDirs);
				SetLocalDirectoriesCache(cx, allDirs, filteredDirs);
			}

			// dirs are not filtered for now
			//Filter(cx, cache_.GetLocalDirectories());
			SortInternal(cx, cache_.GetLocalDirectories());
			cache_.GetLocalDirectories().UpdateLookup();

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

			if (StaleLocalDirectoriesCache(cx))
			{
				// don't use context, it'll filter with extensions, etc.

				var cx2 = new Context(
					"", null, cx.PackagesRoot,
					Context.NoSort, Context.NoSortDirection, cx.Flags, "",
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
					return null;

				if (path == "")
					return this;

				if (cache_ != null)
				{
					IFilesystemObject o;
					if (cache_.Resolve(path, out o))
						return o;
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

		protected abstract List<IFilesystemContainer> DoGetDirectories(Context cx);

		protected virtual bool DoHasDirectories(Context cx)
		{
			bool b = false;

			Instrumentation.Start(I.BasicDoHasDirectories);
			{
				var path = MakeRealPath();

				if (!string.IsNullOrEmpty(path))
				{
					var dirs = Sys.GetDirectories(this, path);
					b = (dirs != null && dirs.Length > 0);
				}
			}
			Instrumentation.End();

			return b;
		}

		protected virtual List<IFilesystemObject> DoGetFiles(Context cx)
		{
			var list = new List<IFilesystemObject>();

			Instrumentation.Start(I.BasicDoGetFiles);
			{
				var path = MakeRealPath();

				if (!string.IsNullOrEmpty(path))
				{
					foreach (var filePath in Sys.GetFiles(this, path))
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


			string rvp;

			Instrumentation.Start(I.IncludeDirectoryGetRVP);
			{
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

		private List<IFilesystemObject> GetFilesInternal(Context cx)
		{
			List<IFilesystemObject> files;

			Instrumentation.Start(I.CallingDoGetFiles);
			{
				// unset recursive, this is just for this container
				bool oldRecursive = cx.Recursive;
				cx.Recursive = false;
				files = DoGetFiles(cx);
				cx.Recursive = oldRecursive;
			}
			Instrumentation.End();

			Instrumentation.Start(I.CheckFiles);
			{
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
			}
			Instrumentation.End();

			return files;
		}

		private void Filter<EntryType>(Context cx, Listing<EntryType> listing)
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
							if (cx.ExtensionMatches(f.DisplayName))
								perExtension.Add(f);
						}
					}

					listing.SetExtensions(cx, perExtension);
				}
			}

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
	}
}
