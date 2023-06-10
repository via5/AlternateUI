using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

#if MOCK
using System.IO;
#endif


namespace AUI
{
	using FMS = FileManagerSecure;

	public interface ISys
	{
		void CreateDirectory(string path);
		bool FileExists(string path);
		void DeleteFile(string path);
		string[] GetDirectories(string path);
		string[] GetFiles(string path);
		List<ShortCut> GetShortCutsForDirectory(string path);
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

		MVRPluginManager GetPluginManager();
		MVRPluginUI GetPluginUI();
		string GetPluginPath();
		void StartCoroutine(System.Collections.IEnumerator e);
		void SetPluginEnabled(bool b);
		JSONNode LoadJSON(string file);
		void SaveJSON(JSONClass n, string file);
		UIDynamicToggle CreateToggle(JSONStorableBool s, bool rightSide = false);
		UIDynamicTextField CreateTextField(JSONStorableString s, bool rightSide = false);
		UIDynamicButton CreateButton(string text, bool rightSide = false);
		UIDynamic CreateSpacer(bool rightSide = false);

		void LogMessage(string s);
		void LogError(string s);
	}

	public class Sys
	{
		public static DateTime BadDateTime = DateTime.MaxValue;
	}


#if !MOCK
	class VamSys : Sys, ISys
	{
		private readonly MVRScript s_;

		public VamSys(MVRScript s)
		{
			s_ = s;
		}

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

		public List<ShortCut> GetShortCutsForDirectory(string path)
		{
			return FMS.GetShortCutsForDirectory(path);
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


		public MVRPluginManager GetPluginManager()
		{
			return s_.manager;
		}

		public MVRPluginUI GetPluginUI()
		{
			Transform p = s_.enabledJSON?.toggle?.transform;

			while (p != null)
			{
				var pui = p.GetComponent<MVRPluginUI>();
				if (pui != null)
					return pui;

				p = p.parent;
			}

			return null;
		}

		public string GetPluginPath()
		{
			// based on MacGruber, which was based on VAMDeluxe, which was
			// in turn based on Alazi

			string id = s_.name.Substring(0, s_.name.IndexOf('_'));
			string filename = s_.manager.GetJSON()["plugins"][id].Value;

			var path = filename.Substring(
				0, filename.LastIndexOfAny(new char[] { '/', '\\' }));

			path = path.Replace('/', '\\');
			if (path.EndsWith("\\"))
				path = path.Substring(0, path.Length - 1);

			return path;
		}

		public void StartCoroutine(System.Collections.IEnumerator e)
		{
			s_.StartCoroutine(e);
		}

		public void SetPluginEnabled(bool b)
		{
			s_.enabledJSON.val = b;
		}

		public JSONNode LoadJSON(string file)
		{
			return s_.LoadJSON(file);
		}

		public void SaveJSON(JSONClass n, string file)
		{
			s_.SaveJSON(n, file);
		}

		public UIDynamicToggle CreateToggle(JSONStorableBool s, bool rightSide = false)
		{
			return s_.CreateToggle(s, rightSide);
		}

		public UIDynamicTextField CreateTextField(JSONStorableString s, bool rightSide = false)
		{
			return s_.CreateTextField(s, rightSide);
		}

		public UIDynamicButton CreateButton(string text, bool rightSide = false)
		{
			return s_.CreateButton(text, rightSide);
		}

		public UIDynamic CreateSpacer(bool rightSide = false)
		{
			return s_.CreateSpacer(rightSide);
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

#else
	public class FSSys : Sys, ISys
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

		public List<ShortCut> GetShortCutsForDirectory(string path)
		{
			var list = new List<ShortCut>();

			foreach (var d in new DirectoryInfo(packages_).GetDirectories())
			{
				if (new DirectoryInfo(d.FullName + "\\" + path).Exists)
				{
					var sc = new ShortCut();
					sc.package = new DirectoryInfo(d.FullName).Name;
					sc.path = d.Name + ":/" + path;
					sc.packageFilter = "";
					sc.flatten = false;
					sc.isLatest = true;

					list.Add(sc);
				}
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


		public MVRPluginManager GetPluginManager()
		{
			return null;
		}

		public MVRPluginUI GetPluginUI()
		{
			return null;
		}

		public string GetPluginPath()
		{
			return "";
		}

		public void StartCoroutine(System.Collections.IEnumerator e)
		{
		}

		public void SetPluginEnabled(bool b)
		{
		}

		public JSONNode LoadJSON(string file)
		{
			return new JSONClass();
		}

		public void SaveJSON(JSONClass n, string file)
		{
		}

		public UIDynamicToggle CreateToggle(JSONStorableBool s, bool rightSide = false)
		{
			return null;
		}

		public UIDynamicTextField CreateTextField(JSONStorableString s, bool rightSide = false)
		{
			return null;
		}

		public UIDynamicButton CreateButton(string text, bool rightSide = false)
		{
			return null;
		}

		public UIDynamic CreateSpacer(bool rightSide = false)
		{
			return null;
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


	public class VFSSys : Sys, ISys
	{
		public class Package
		{
			public string name;
			public readonly List<string> dirs = new List<string>();
			public readonly List<string> files = new List<string>();
		}

		private readonly List<string> dirs_ = new List<string>();
		private readonly List<string> files_ = new List<string>();
		private readonly Dictionary<string, Package> packages_ = new Dictionary<string, Package>();

		public VFSSys()
		{
		}

		public void AddDir(string file)
		{
			dirs_.Add(file);
		}

		public void AddFile(string file)
		{
			files_.Add(file);
		}

		public void AddPackage(Package p)
		{
			packages_.Add(p.name, p);
		}

		public void RemovePackage(string name)
		{
			packages_.Remove(name);
		}

		public void CreateDirectory(string path)
		{
			Console.WriteLine($"vfssys: would create {path}");
		}

		public bool FileExists(string path)
		{
			return new FileInfo(path).Exists;
		}

		public void DeleteFile(string path)
		{
			Console.WriteLine($"vfssys: would delete {path}");
		}

		public string[] GetDirectories(string path)
		{
			if (path.IndexOf(":/") != -1)
				return GetDirectoriesInPackage(path);

			if (!path.EndsWith("/"))
				path += "/";

			var list = new List<string>();

			foreach (var f in dirs_)
			{
				if (f.StartsWith(path))
				{
					int nextSep = f.IndexOf('/', path.Length);
					int nextNextSep = f.IndexOf('/', nextSep + 1);

					if (nextNextSep == -1)
						list.Add(f.Substring(0, f.Length - 1));
				}
			}

			return list.ToArray();
		}

		public string[] GetFiles(string path)
		{
			if (path.IndexOf(":/") != -1)
				return GetFilesInPackage(path);

			if (!path.EndsWith("/"))
				path += "/";

			var list = new List<string>();

			foreach (var f in files_)
			{
				if (f.StartsWith(path))
					list.Add(f);
			}

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
			pp.path = path.Substring(i + 2);//.Replace('/', '\\');

			return pp;
		}

		private string[] GetDirectoriesInPackage(string path)
		{
			var pp = ParsePackagePath(path);

			Package p;
			if (!packages_.TryGetValue(pp.name, out p))
				throw new Exception($"GetDirectoriesInPackage for non-existent package '{pp.name}'");

			var list = new List<string>();
			var ppath = pp.path;

			if (!ppath.EndsWith("/"))
				ppath += "/";

			foreach (var f in p.dirs)
			{
				if (f.StartsWith(ppath))
				{
					int nextSep = f.IndexOf('/', ppath.Length);
					int nextNextSep = f.IndexOf('/', nextSep + 1);

					if (nextNextSep == -1)
						list.Add(f.Substring(0, f.Length - 1));
				}
			}

			return list.ToArray();
		}

		private string[] GetFilesInPackage(string path)
		{
			var pp = ParsePackagePath(path);

			Package p;
			if (!packages_.TryGetValue(pp.name, out p))
				throw new Exception($"GetFilesInPackage for non-existent package '{pp.name}'");

			var list = new List<string>();

			var ppath = pp.path;
			if (!ppath.EndsWith("/"))
				ppath += "/";

			foreach (var f in p.files)
			{
				if (f.StartsWith(ppath))
					list.Add(f);
			}

			return list.ToArray();
		}

		public List<ShortCut> GetShortCutsForDirectory(string path)
		{
			var list = new List<ShortCut>();

			if (!path.EndsWith("/"))
				path += "/";

			foreach (var p in packages_)
			{
				foreach (var d in p.Value.dirs)
				{
					if (d.StartsWith(path))
					{
						var sc = new ShortCut();
						sc.package = p.Value.name;
						sc.path = p.Value.name + ":/" + path.Substring(0, path.Length - 1).Replace('\\', '/');
						sc.packageFilter = "";
						sc.flatten = false;
						sc.isLatest = true;

						list.Add(sc);

						break;
					}
				}
			}

			return list;
		}

		public DateTime FileCreationTime(string path)
		{
			return BadDateTime;
		}

		public DateTime FileLastWriteTime(string path)
		{
			return BadDateTime;
		}

		public DateTime DirectoryCreationTime(string path)
		{
			return BadDateTime;
		}

		public DateTime DirectoryLastWriteTime(string path)
		{
			return BadDateTime;
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


		public MVRPluginManager GetPluginManager()
		{
			return null;
		}

		public MVRPluginUI GetPluginUI()
		{
			return null;
		}

		public string GetPluginPath()
		{
			return "";
		}

		public void StartCoroutine(System.Collections.IEnumerator e)
		{
		}

		public void SetPluginEnabled(bool b)
		{
		}

		public JSONNode LoadJSON(string file)
		{
			return new JSONClass();
		}

		public void SaveJSON(JSONClass n, string file)
		{
		}

		public UIDynamicToggle CreateToggle(JSONStorableBool s, bool rightSide = false)
		{
			return null;
		}

		public UIDynamicTextField CreateTextField(JSONStorableString s, bool rightSide = false)
		{
			return null;
		}

		public UIDynamicButton CreateButton(string text, bool rightSide = false)
		{
			return null;
		}

		public UIDynamic CreateSpacer(bool rightSide = false)
		{
			return null;
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
