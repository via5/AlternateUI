using System;
using System.Collections.Generic;

namespace AUI.FS
{
	class AllFlatDirectory : BasicFilesystemContainer
	{
		public AllFlatDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "All flattened")
		{
		}

		public override string ToString()
		{
			return "AllFlatDirectory";
		}

		public override DateTime DateCreated
		{
			get { return DateTime.MaxValue; }
		}

		public override DateTime DateModified
		{
			get { return DateTime.MaxValue; }
		}

		public override VUI.Icon Icon
		{
			get { return Icons.Get(Icons.Directory); }
		}

		public override bool CanPin
		{
			get { return false; }
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override bool ChildrenVirtual
		{
			get { return false; }
		}

		public override bool IsFlattened
		{
			get { return true; }
		}

		public override bool IsInternal
		{
			get { return true; }
		}

		public override string MakeRealPath()
		{
			return "";
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			return fs_.GetRoot().GetDirectoriesForFlatten(cx);
		}

		protected override bool DoHasDirectories(Context cx)
		{
			return false;
		}
	}


	class PackagesFlatDirectory : BasicFilesystemContainer
	{
		public PackagesFlatDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Packages flattened")
		{
		}

		public override string Name
		{
			get { return "Packages flattened"; }
		}

		public override string ToString()
		{
			return "PackagesFlatDirectory";
		}

		public override DateTime DateCreated
		{
			get { return DateTime.MaxValue; }
		}

		public override DateTime DateModified
		{
			get { return DateTime.MaxValue; }
		}

		public override VUI.Icon Icon
		{
			get { return Icons.Get(Icons.Package); }
		}

		public override bool CanPin
		{
			get { return false; }
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override bool ChildrenVirtual
		{
			get { return true; }
		}

		public override bool IsFlattened
		{
			get { return true; }
		}

		public override bool IsInternal
		{
			get { return true; }
		}

		public override string MakeRealPath()
		{
			return "";
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			return fs_.GetPackagesRoot().GetPackages(cx);
		}

		protected override bool DoHasDirectories(Context cx)
		{
			return false;
		}
	}


	class PinnedFlatDirectory : BasicFilesystemContainer
	{
		public PinnedFlatDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Pinned flattened")
		{
			fs_.PinsChanged += ClearCache;
		}

		public override string ToString()
		{
			return "PinnedFlatDirectory";
		}

		public override DateTime DateCreated
		{
			get { return DateTime.MaxValue; }
		}

		public override DateTime DateModified
		{
			get { return DateTime.MaxValue; }
		}

		public override VUI.Icon Icon
		{
			get { return Icons.Get(Icons.UnpinnedDark); }
		}

		public override bool CanPin
		{
			get { return false; }
		}

		public override bool Virtual
		{
			get { return true; }
		}

		public override bool ChildrenVirtual
		{
			get { return false; }
		}

		public override bool IsFlattened
		{
			get { return true; }
		}

		public override bool IsInternal
		{
			get { return true; }
		}

		public override string MakeRealPath()
		{
			return "";
		}

		protected override List<IFilesystemContainer> DoGetDirectories(Context cx)
		{
			return fs_.GetRoot().PinnedRoot.Pinned;
		}

		protected override bool DoHasDirectories(Context cx)
		{
			return false;
		}
	}
}
