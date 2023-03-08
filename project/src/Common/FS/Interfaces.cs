using System;
using System.Collections.Generic;

namespace AUI.FS
{
	class Listing<EntriesType>
		where EntriesType : class, IFilesystemObject
	{
		private List<EntriesType> raw_ = null;
		private string currentExtensions_ = null;
		private List<EntriesType> perExtension_ = null;
		public string currentSearch_ = null;
		private List<EntriesType> searched_ = null;
		public int currentSort_ = Context.NoSort;
		public int currentSortDir_ = Context.NoSortDirection;
		private List<EntriesType> sorted_ = null;

		public void SetRaw(List<EntriesType> list)
		{
			raw_ = list;
			SetAllStale();
		}

		public void AddRaw(List<EntriesType> list)
		{
			if (raw_ == null)
				raw_ = new List<EntriesType>();

			raw_.AddRange(list);
			SetAllStale();
		}

		public List<EntriesType> Raw
		{
			get { return raw_; }
		}

		public List<EntriesType> PerExtension
		{
			get { return perExtension_; }
		}

		public List<EntriesType> Searched
		{
			get { return searched_; }
		}

		public List<EntriesType> Sorted
		{
			get { return sorted_; }
		}

		public List<EntriesType> Last
		{
			get { return sorted_ ?? searched_ ?? perExtension_ ?? raw_ ?? new List<EntriesType>(); }
		}


		public bool ExtensionsStale(Context cx)
		{
			return (currentExtensions_ != cx.ExtensionsString);
		}

		public void SetExtensions(Context cx, List<EntriesType> list)
		{
			currentExtensions_ = cx.ExtensionsString;
			perExtension_ = list;

			SetSearchStale();
			SetSortStale();
		}


		public bool SearchStale(Context cx)
		{
			return (currentSearch_ != cx.Search);
		}

		public void SetSearched(Context cx, List<EntriesType> list)
		{
			currentSearch_ = cx.Search;
			searched_ = list;
			SetSortStale();
		}


		public bool SortStale(Context cx)
		{
			return (currentSort_ != cx.Sort || currentSortDir_ != cx.SortDirection);
		}

		public void SetSorted(Context cx, List<EntriesType> list)
		{
			currentSort_ = cx.Sort;
			currentSortDir_ = cx.SortDirection;
			sorted_ = list;
		}


		private void SetAllStale()
		{
			SetPerExtensionStale();
			SetSearchStale();
			SetSortStale();
		}

		private void SetPerExtensionStale()
		{
			currentExtensions_ = null;
			perExtension_ = null;
		}

		private void SetSearchStale()
		{
			currentSearch_ = null;
			searched_ = null;
		}

		private void SetSortStale()
		{
			currentSort_ = Context.NoSort;
			currentSortDir_ = Context.NoSortDirection;
			sorted_ = null;
		}
	}


	interface IFilesystemObject
	{
		IFilesystemContainer Parent { get; }

		string Name { get; }
		string VirtualPath { get; }
		string DisplayName { get; set; }
		string Tooltip { get; }
		bool HasCustomDisplayName { get; }
		DateTime DateCreated { get; }
		DateTime DateModified { get; }
		VUI.Icon Icon { get; }

		bool CanPin { get; }
		bool Virtual { get; }
		bool ChildrenVirtual { get; }
		bool IsFlattened { get; }
		bool IsRedundant { get; }
		IPackage ParentPackage { get; }

		string MakeRealPath();
		bool IsSameObject(IFilesystemObject o);
		void ClearCache();
	}


	interface IFilesystemContainer : IFilesystemObject
	{
		bool AlreadySorted { get; }
		bool HasDirectories(Context cx);
		List<IFilesystemContainer> GetDirectories(Context cx);
		List<IFilesystemObject> GetFiles(Context cx);

		void GetFilesRecursiveInternal(
			Context cx, Listing<IFilesystemObject> listing);
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
}
