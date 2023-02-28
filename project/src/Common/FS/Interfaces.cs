using System;
using System.Collections.Generic;

namespace AUI.FS
{
	interface IFilesystemObject
	{
		IFilesystemContainer Parent { get; }

		string Name { get; }
		string VirtualPath { get; }
		string DisplayName { get; set; }
		bool HasCustomDisplayName { get; }
		DateTime DateCreated { get; }
		DateTime DateModified { get; }
		Icon Icon { get; }

		bool CanPin { get; }
		bool Virtual { get; }
		bool ChildrenVirtual { get; }
		bool IsFlattened { get; }
		IPackage ParentPackage { get; }

		string MakeRealPath();
		bool IsSameObject(IFilesystemObject o);
	}


	interface IFilesystemContainer : IFilesystemObject
	{
		bool HasSubDirectories(Filter filter);
		List<IFilesystemContainer> GetSubDirectories(Filter filter);
		List<IFilesystemObject> GetFiles(Filter filter);
		List<IFilesystemObject> GetFilesRecursive(Filter filter);
		void GetFilesRecursiveUnfiltered(List<IFilesystemObject> list);
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
