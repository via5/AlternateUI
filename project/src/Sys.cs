using MVR.FileManagement;
using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;
using UnityEngine;

#if MOCK
using System.IO;
#endif


namespace AUI
{
	using FMS = FileManagerSecure;

	interface ISysShortCut
	{
		string Package { get; }
		string PackageFilter { get; }
		string Path { get; }
		bool Flatten { get; }
		bool IsLatest { get; }
		bool IsHidden { get; }
	}

	class VamSysShortCut : ISysShortCut
	{
		private readonly ShortCut sc_;

		public VamSysShortCut(ShortCut sc)
		{
			sc_ = sc;
		}

		public string Package
		{
			get { return sc_.package; }
		}

		public string PackageFilter
		{
			get { return sc_.packageFilter; }
		}

		public string Path
		{
			get { return sc_.path; }
		}

		public bool Flatten
		{
			get { return sc_.flatten; }
		}

		public bool IsLatest
		{
			get { return sc_.isLatest; }
		}

		public bool IsHidden
		{
			get { return sc_.isHidden; }
		}
	}


#if MOCK
	class FSSysShortCut : ISysShortCut
	{
		private readonly string dir_, path_;

		public FSSysShortCut(string dir, string path)
		{
			dir_ = dir;
			path_ = path;
		}

		public string Package
		{
			get { return new DirectoryInfo(dir_).Name; }
		}

		public string PackageFilter
		{
			get { return ""; }
		}

		public string Path
		{
			get { return path_; }
		}

		public bool Flatten
		{
			get { return false; }
		}

		public bool IsLatest
		{
			get { return true; }
		}

		public bool IsHidden
		{
			get { return false; }
		}
	}
#endif



	interface ISys
	{
		void CreateDirectory(string path);
		bool FileExists(string path);
		void DeleteFile(string path);
		string[] GetDirectories(string path);
		string[] GetFiles(string path);
		List<ISysShortCut> GetShortCutsForDirectory(string path);
		DateTime FileCreationTime(string path);
		DateTime FileLastWriteTime(string path);
		DateTime DirectoryCreationTime(string path);
		DateTime DirectoryLastWriteTime(string path);
		bool IsDirectoryInPackage(string path);
		string NormalizePath(string path);

		bool GetKeyUp(KeyCode c);
		bool GetKey(KeyCode c);

		float GetDeltaTime();
		float GetRealtimeSinceStartup();

		void LogMessage(string s);
		void LogError(string s);
	}

	class Sys
	{
		public static DateTime BadDateTime = DateTime.MaxValue;
	}


	class VamSys : Sys, ISys
	{
		public void CreateDirectory(string path)
		{
			FMS.CreateDirectory(path);
		}

		public bool FileExists(string path)
		{
			return FMS.FileExists(path);
		}

		public void DeleteFile(string path)
		{
			FMS.DeleteFile(path);
		}

		public string[] GetDirectories(string path)
		{
			return FMS.GetDirectories(path);
		}

		public string[] GetFiles(string path)
		{
			return FMS.GetFiles(path);
		}

		public List<ISysShortCut> GetShortCutsForDirectory(string path)
		{
			var list = new List<ISysShortCut>();

			foreach (var sc in FMS.GetShortCutsForDirectory(path))
				list.Add(new VamSysShortCut(sc));

			return list;
		}

		public DateTime FileCreationTime(string path)
		{
			return FMS.FileCreationTime(path);
		}

		public DateTime FileLastWriteTime(string path)
		{
			return FMS.FileLastWriteTime(path);
		}

		public DateTime DirectoryCreationTime(string path)
		{
#if VAM_GT_1_22
			return FMS.DirectoryCreationTime(path);
#else
			return BadDateTime;
#endif
		}

		public DateTime DirectoryLastWriteTime(string path)
		{
			return FMS.DirectoryLastWriteTime(path);
		}

		public bool IsDirectoryInPackage(string path)
		{
			return FMS.IsDirectoryInPackage(path);
		}

		public string NormalizePath(string path)
		{
			return FMS.NormalizePath(path);
		}



		public bool GetKeyUp(KeyCode c)
		{
			return Input.GetKeyUp(c);
		}

		public bool GetKey(KeyCode c)
		{
			return Input.GetKey(c);
		}


		public float GetDeltaTime()
		{
			return Time.deltaTime;
		}

		public float GetRealtimeSinceStartup()
		{
			return Time.realtimeSinceStartup;
		}


		public void LogMessage(string s)
		{
			SuperController.LogMessage(s);
		}

		public void LogError(string s)
		{
			SuperController.LogMessage(s);
		}
	}


#if MOCK
	class FSSys : Sys, ISys
	{
		private readonly string root_, packages_;

		public FSSys(string root, string packages)
		{
			root_ = root;
			packages_ = packages;
		}

		private string MakePath(string p)
		{
			return root_ + "\\" + p;
		}

		public void CreateDirectory(string path)
		{
			Console.WriteLine($"fssys: would create {MakePath(path)}");
		}

		public bool FileExists(string path)
		{
			return new FileInfo(MakePath(path)).Exists;
		}

		public void DeleteFile(string path)
		{
			Console.WriteLine($"fssys: would delete {MakePath(path)}");
		}

		public string[] GetDirectories(string path)
		{
			if (path.IndexOf(":/") != -1)
				return GetDirectoriesInPackage(path);

			var list = new List<string>();

			foreach (var d in new DirectoryInfo(MakePath(path)).GetDirectories())
				list.Add(d.Name);

			return list.ToArray();
		}

		public string[] GetFiles(string path)
		{
			if (path.IndexOf(":/") != -1)
				return GetFilesInPackage(path);

			var list = new List<string>();

			foreach (var d in new DirectoryInfo(MakePath(path)).GetFiles())
				list.Add(d.Name);

			return list.ToArray();
		}

		struct PackagePath
		{
			public string name, path;
		}

		private PackagePath ParsePackagePath(string path)
		{
			int i = path.IndexOf(":/");

			var pp = new PackagePath();
			pp.name = path.Substring(0, i);
			pp.path = path.Substring(i + 2).Replace('/', '\\');

			return pp;
		}

		private string[] GetDirectoriesInPackage(string path)
		{
			var pp = ParsePackagePath(path);

			foreach (var d in new DirectoryInfo(packages_).GetDirectories())
			{
				if (d.Name == pp.name)
				{
					var list = new List<string>();

					foreach (var dd in new DirectoryInfo(d.FullName + "\\" + pp.path).GetDirectories())
						list.Add(dd.Name);

					return list.ToArray();
				}
			}

			return new string[0];
		}

		private string[] GetFilesInPackage(string path)
		{
			var pp = ParsePackagePath(path);

			foreach (var d in new DirectoryInfo(packages_).GetDirectories())
			{
				if (d.Name == pp.name)
				{
					var list = new List<string>();

					foreach (var dd in new DirectoryInfo(d.FullName + "\\" + pp.path).GetFiles())
						list.Add(dd.Name);

					return list.ToArray();
				}
			}

			return new string[0];
		}

		public List<ISysShortCut> GetShortCutsForDirectory(string path)
		{
			var list = new List<ISysShortCut>();

			foreach (var d in new DirectoryInfo(packages_).GetDirectories())
			{
				if (new DirectoryInfo(d.FullName + "\\" + path).Exists)
					list.Add(new FSSysShortCut(d.FullName, d.Name + ":/" + path));
			}

			return list;
		}

		public DateTime FileCreationTime(string path)
		{
			return new FileInfo(MakePath(path)).CreationTime;
		}

		public DateTime FileLastWriteTime(string path)
		{
			return new FileInfo(MakePath(path)).LastWriteTime;
		}

		public DateTime DirectoryCreationTime(string path)
		{
			return new DirectoryInfo(MakePath(path)).CreationTime;
		}

		public DateTime DirectoryLastWriteTime(string path)
		{
			return new DirectoryInfo(MakePath(path)).LastWriteTime;
		}

		public bool IsDirectoryInPackage(string path)
		{
			return false;
		}

		public string NormalizePath(string path)
		{
			return path;
		}


		public bool GetKeyUp(KeyCode c)
		{
			return false;
		}

		public bool GetKey(KeyCode c)
		{
			return false;
		}


		public float GetDeltaTime()
		{
			return 0;
		}

		public float GetRealtimeSinceStartup()
		{
			return 0;
		}


		public void LogMessage(string s)
		{
			Console.WriteLine(s);
		}

		public void LogError(string s)
		{
			Console.WriteLine(s);
		}
	}
#endif
}
