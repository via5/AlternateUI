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

	abstract class BasicFilesystemContainer : BasicFilesystemObject, IFilesystemContainer
	{
		private readonly string name_;

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
		}

		public virtual bool AlreadySorted
		{
			get { return false; }
		}


		public bool HasDirectories(Context cx)
		{
			return (GetDirectories(cx).Count > 0);
		}

		public List<IFilesystemContainer> GetDirectories(Context cx)
		{
			var listing = new Listing<IFilesystemContainer>();

			var list = DoGetDirectories(cx);
			if (list != null)
				listing.SetRaw(list);

			Filter(cx, listing);

			return listing.Last;
		}

		public List<IFilesystemObject> GetFiles(Context cx)
		{
			var listing = new Listing<IFilesystemObject>();

			if (cx.Recursive || IsFlattened)
			{
				GetFilesRecursiveInternal(cx, listing);
			}
			else
			{
				var list = DoGetFiles(cx);
				if (list != null)
					listing.SetRaw(list);
			}

			Filter(cx, listing);

			return listing.Last;
		}

		public void GetFilesRecursiveInternal(
			Context cx, Listing<IFilesystemObject> listing)
		{
			var list = DoGetFiles(cx);
			if (list != null)
				listing.AddRaw(list);

			var dirs = DoGetDirectories(cx);

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


		protected virtual List<IFilesystemContainer> GetDirectories()
		{
			var list = new List<IFilesystemContainer>();

			foreach (var dirPath in GetDirectoriesFromFMS(MakeRealPath()))
				list.Add(new FSDirectory(fs_, this, Path.Filename(dirPath)));

			return list;
		}

		protected virtual List<IFilesystemObject> GetFiles()
		{
			var list = new List<IFilesystemObject>();

			foreach (var filePath in GetFilesFromFMS(MakeRealPath()))
				list.Add(new FSFile(fs_, this, Path.Filename(filePath)));

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


		private List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			var dirs = GetDirectories();

			if (dirs != null && !cx.ShowHiddenFolders)
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

		private List<IFilesystemObject> DoGetFiles(Context cx)
		{
			var files = GetFiles();

			if (files != null && !cx.ShowHiddenFiles)
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

			DoSort(cx, listing);
		}

		private void DoSort<EntryType>(Context cx, Listing<EntryType> listing)
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
				AlternateUI.Instance.Log.ErrorST($"bad directory '{path}': {e.Message}");
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
				AlternateUI.Instance.Log.ErrorST($"bad directory '{path}': {e.Message}");
				return new string[0];
			}
		}
	}
}
