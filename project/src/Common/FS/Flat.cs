﻿using System.Collections.Generic;

namespace AUI.FS
{
	public class AllFlatDirectory : BasicFilesystemContainer
	{
		public AllFlatDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "All flattened")
		{
		}

		protected override string DoGetDebugName()
		{
			return "AllFlatDirectory";
		}

		protected override VUI.Icon GetIcon()
		{
			return Icons.GetIcon(Icons.Directory);
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

		public override bool IsWritable
		{
			get { return false; }
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


	public class PackagesFlatDirectory : BasicFilesystemContainer
	{
		public PackagesFlatDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Packages flattened")
		{
		}

		public override string Name
		{
			get { return "Packages flattened"; }
		}

		protected override string DoGetDebugName()
		{
			return "PackagesFlatDirectory";
		}

		protected override VUI.Icon GetIcon()
		{
			return Icons.GetIcon(Icons.PackageDark);
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

		public override bool IsWritable
		{
			get { return false; }
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


	public class PinnedFlatDirectory : BasicFilesystemContainer
	{
		public PinnedFlatDirectory(Filesystem fs, IFilesystemContainer parent)
			: base(fs, parent, "Pinned flattened")
		{
			fs_.PinsChanged += ClearCache;
		}

		protected override string DoGetDebugName()
		{
			return "PinnedFlatDirectory";
		}

		protected override VUI.Icon GetIcon()
		{
			return Icons.GetIcon(Icons.UnpinnedDark);
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

		public override bool IsWritable
		{
			get { return false; }
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
