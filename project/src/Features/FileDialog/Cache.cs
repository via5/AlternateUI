using MVR.FileManagementSecure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AUI.FileDialog
{
	static class Cache
	{
		class CacheInfo
		{
			private readonly string parent_;
			private readonly string normalizedParent_;
			private readonly string[] exts_;
			private readonly int sort_, sortDir_;

			public CacheInfo(string parent, Filter f)
			{
				parent_ = parent;
				normalizedParent_ = parent.Replace('\\', '/');
				exts_ = f?.Extensions;
				sort_ = f?.Sort ?? Filter.NoSort;
				sortDir_ = f?.SortDirection ?? Filter.SortAscending;
			}

			public override int GetHashCode()
			{
				return HashHelper.Combine(
					normalizedParent_.GetHashCode(),
					HashHelper.HashArray(exts_),
					sort_, sortDir_);
			}

			public override bool Equals(object o)
			{
				return Equals(o as CacheInfo);
			}

			public bool Equals(CacheInfo o)
			{
				return
					normalizedParent_ == o.normalizedParent_ &&
					ArraysEqual(exts_, o.exts_) &&
					sort_ == o.sort_ &&
					sortDir_ == o.sortDir_;
			}

			private bool ArraysEqual<T>(T[] a, T[] b)
			{
				if (a == b)
					return true;

				if (a == null || b == null)
					return false;

				return Enumerable.SequenceEqual(a, b);
			}
		}


		public class Filter
		{
			public const int NoSort = 0;
			public const int SortFilename = 1;
			public const int SortType = 2;
			public const int SortDateModified = 3;
			public const int SortDateCreated = 4;

			public const int SortAscending = 0;
			public const int SortDescending = 1;

			private readonly string search_;
			private readonly string searchLc_;
			private readonly string[] exts_;
			private readonly Regex searchRe_;
			private readonly int sort_;
			private readonly int sortDir_;

			public Filter(string search, string[] extensions, int sort, int sortDir)
			{
				search_ = search;
				exts_ = extensions;
				sort_ = sort;
				sortDir_ = sortDir;

				if (VUI.Utilities.IsRegex(search_))
				{
					searchLc_ = null;
					searchRe_ = VUI.Utilities.CreateRegex(search_);
				}
				else
				{
					searchLc_ = search.ToLower();
					searchRe_ = null;
				}
			}

			public string Search
			{
				get { return search_; }
			}

			public string[] Extensions
			{
				get { return exts_; }
			}

			public int Sort
			{
				get { return sort_; }
			}

			public int SortDirection
			{
				get { return sortDir_; }
			}

			public bool ExtensionMatches(string path)
			{
				if (exts_ != null)
				{
					foreach (var e in exts_)
					{
						if (path.EndsWith(e))
							return true;
					}

					return false;
				}

				return true;
			}

			public bool SearchMatches(IFile f)
			{
				if (searchLc_ != null)
				{
					if (f.Name.ToLower().IndexOf(searchLc_) == -1)
						return false;
				}
				else if (searchRe_ != null)
				{
					if (!searchRe_.IsMatch(f.Name))
						return false;
				}

				return true;
			}


			public class FilenameComparer : IComparer<IFile>
			{
				private readonly int dir_;

				public FilenameComparer(int dir)
				{
					dir_ = dir;
				}

				public static int SCompare(IFile a, IFile b, int dir)
				{
					if (dir == SortAscending)
						return U.CompareNatural(a.Name, b.Name);
					else
						return U.CompareNatural(b.Name, a.Name);
				}

				public int Compare(IFile a, IFile b)
				{
					return SCompare(a, b, dir_);
				}
			}

			public class TypeComparer : IComparer<IFile>
			{
				private readonly int dir_;

				public TypeComparer(int dir)
				{
					dir_ = dir;
				}

				public int Compare(IFile a, IFile b)
				{
					int c;

					if (dir_ == SortAscending)
						c = U.CompareNatural(a.Extension, b.Extension);
					else
						c = U.CompareNatural(b.Extension, a.Extension);

					if (c == 0)
						c = FilenameComparer.SCompare(a, b, dir_);

					return c;
				}
			}

			public class DateModifiedComparer : IComparer<IFile>
			{
				private readonly int dir_;

				public DateModifiedComparer(int dir)
				{
					dir_ = dir;
				}

				public int Compare(IFile a, IFile b)
				{
					int c;

					if (dir_ == SortAscending)
						c = DateTime.Compare(a.DateModified, b.DateModified);
					else
						c = DateTime.Compare(b.DateModified, a.DateModified);

					if (c == 0)
						c = FilenameComparer.SCompare(a, b, dir_);

					return c;
				}
			}

			public class DateCreatedComparer : IComparer<IFile>
			{
				private readonly int dir_;

				public DateCreatedComparer(int dir)
				{
					dir_ = dir;
				}

				public int Compare(IFile a, IFile b)
				{
					int c;

					if (dir_ == SortAscending)
						c = DateTime.Compare(a.DateCreated, b.DateCreated);
					else
						c = DateTime.Compare(b.DateCreated, a.DateCreated);

					if (c == 0)
						c = FilenameComparer.SCompare(a, b, dir_);

					return c;
				}
			}

			public void SortList(List<IFile> list)
			{
				switch (sort_)
				{
					case SortFilename:
					{
						list.Sort(new FilenameComparer(sortDir_));
						break;
					}

					case SortType:
					{
						list.Sort(new TypeComparer(sortDir_));
						break;
					}

					case SortDateModified:
					{
						list.Sort(new DateModifiedComparer(sortDir_));
						break;
					}

					case SortDateCreated:
					{
						list.Sort(new DateCreatedComparer(sortDir_));
						break;
					}

					case NoSort:
					{
						// no-op
						break;
					}
				}
			}
		}


		public struct Extension
		{
			public string name;
			public string ext;

			public Extension(string name, string ext)
			{
				this.name = name;
				this.ext = ext;
			}
		}


		public static string SavesRoot = "Saves";
		public static string ScenesRoot = "Saves/scene";

		public static string DefaultSceneExtension = ".json";

		public static Extension[] SceneExtensions = new Extension[]
		{
			new Extension("Scenes", ".json"),
			new Extension("VAC files", ".vac"),
			new Extension("Zip files", ".zip"),
		};


		private static readonly Dictionary<CacheInfo, List<IFile>> files_ =
			new Dictionary<CacheInfo, List<IFile>>();

		private static readonly Dictionary<CacheInfo, List<IFile>> dirs_ =
			new Dictionary<CacheInfo, List<IFile>>();

		private static readonly Dictionary<CacheInfo, List<IFile>> packages_ =
			new Dictionary<CacheInfo, List<IFile>>();

		private static List<IFile> packagesFlat_ = null;


		public static List<IFile> GetDirectories(string parent, Filter filter)
		{
			List<IFile> all;
			var ci = new CacheInfo(parent, filter);

			if (!dirs_.TryGetValue(ci, out all))
			{
				all = new List<IFile>();
				foreach (var d in FileManagerSecure.GetDirectories(parent))
				{
					if (filter == null || filter.ExtensionMatches(d))
						all.Add(new File(d));
				}

				U.NatSort(all);
				dirs_.Add(ci, all);
			}

			List<IFile> list;

			if (filter == null || filter.Search == "")
			{
				list = all;
			}
			else
			{
				list = new List<IFile>();

				foreach (var f in all)
				{
					if (filter.SearchMatches(f))
						list.Add(f);
				}
			}

			return list;
		}

		public static bool HasDirectories(string parent, Filter filter)
		{
			var list = GetDirectories(parent, filter);
			return (list.Count > 0);
		}

		public static List<IFile> GetFiles(string parent, Filter filter)
		{
			List<IFile> all = null;
			var ci = new CacheInfo(parent, filter);

			if (!files_.TryGetValue(ci, out all))
			{
				all = new List<IFile>();

				foreach (var f in FileManagerSecure.GetFiles(parent))
				{
					if (filter == null || filter.ExtensionMatches(f))
						all.Add(new File(f));
				}

				filter.SortList(all);
				files_.Add(ci, all);
			}

			List<IFile> list;

			if (filter == null || filter.Search == "")
			{
				list = all;
			}
			else
			{
				list = new List<IFile>();

				foreach (var f in all)
				{
					if (filter.SearchMatches(f))
						list.Add(f);
				}
			}

			return list;
		}

		public static List<IFile> GetFilesRecursive(string parent, Filter filter)
		{
			var list = new List<IFile>();
			GetFilesRecursive(parent, filter, list);
			return list;
		}

		private static void GetFilesRecursive(string parent, Filter filter, List<IFile> list)
		{
			list.AddRange(GetFiles(parent, filter));

			foreach (var d in Cache.GetDirectories(parent, null))
				GetFilesRecursive(d.Path, filter, list);
		}

		public static bool HasPackages(string parent)
		{
			var scs = FileManagerSecure.GetShortCutsForDirectory(parent);
			return (scs != null && scs.Count > 0);
		}

		public static bool DirectoryInPackage(string dir)
		{
			return FileManagerSecure.IsDirectoryInPackage(dir);
		}

		public static List<IFile> GetPackagesFlat(string parent, Filter filter)
		{
			if (packagesFlat_ == null)
			{
				packagesFlat_ = new List<IFile>();

				foreach (var p in FileManagerSecure.GetShortCutsForDirectory(parent))
				{
					if (string.IsNullOrEmpty(p.package))
						continue;

					if (!string.IsNullOrEmpty(p.packageFilter))
						continue;

					GetFilesRecursive(p.path, filter, packagesFlat_);
				}
			}

			return packagesFlat_;
		}

		public static List<IFile> GetPackages(string parent, Filter filter)
		{
			var ci = new CacheInfo(parent, filter);
			List<IFile> list;

			if (!packages_.TryGetValue(ci, out list))
			{
				foreach (var sc in FileManagerSecure.GetShortCutsForDirectory(parent))
					list.Add(new Package(sc));

				packages_.Add(ci, list);
			}

			filter.SortList(list);

			return list;
		}

		public static IFile GetPackage(string path)
		{
			foreach (var p in GetPackages(ScenesRoot, null))
			{
				if (p.Path == path)
					return p;
			}

			return null;
		}
	}

	public static class Icons
	{
		class Icon
		{
			public Texture texture = null;
			public readonly List<Action<Texture>> callbacks_ = new List<Action<Texture>>();

			public Icon(string path)
				: this(path, 0, 0)
			{
			}

			public Icon(string path, int w, int h)
			{
				ImageLoaderThreaded.QueuedImage q = new ImageLoaderThreaded.QueuedImage();
				q.imgPath = path;

				q.callback = (tt) =>
				{
					var tex = tt.tex;

					if (w != 0 && h != 0)
						tex = ScaleTexture(tex, w, h);

					tex.wrapMode = TextureWrapMode.Clamp;

					texture = tex;

					foreach (var f in callbacks_)
						f(texture);

					callbacks_.Clear();
				};

				//ImageLoaderThreaded.singleton.ClearCacheThumbnail(q.imgPath);
				ImageLoaderThreaded.singleton.QueueThumbnail(q);
			}

			public void Get(Action<Texture> f)
			{
				if (texture != null)
					f(texture);
				else
					callbacks_.Add(f);
			}

			private static Texture2D ScaleTexture(Texture src, int width, int height)
			{
				RenderTexture rt = RenderTexture.GetTemporary(width, height);
				Graphics.Blit(src, rt);

				RenderTexture currentActiveRT = RenderTexture.active;
				RenderTexture.active = rt;
				Texture2D tex = new Texture2D(rt.width, rt.height);

				tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
				tex.Apply();

				RenderTexture.ReleaseTemporary(rt);
				RenderTexture.active = currentActiveRT;

				return tex;
			}
		}

		private static Icon package_ = null;
		private static Icon pinned_ = null;
		private static Icon resizeWE_ = null;

		public static void LoadAll()
		{
			package_ = new Icon(AlternateUI.Instance.PluginPath + "/res/icons/package.png");
			pinned_ = new Icon(AlternateUI.Instance.PluginPath + "/res/icons/pinned.png");
			resizeWE_ = new Icon(AlternateUI.Instance.PluginPath + "/res/cursors/resize_w_e.png", 40, 40);
		}

		public static void GetPackageIcon(Action<Texture> f)
		{
			package_.Get(f);
		}

		public static void GetPinnedIcon(Action<Texture> f)
		{
			pinned_.Get(f);
		}

		public static void GetResizeWE(Action<Texture> f)
		{
			resizeWE_.Get(f);
		}

		public static void GetDirectoryIcon(Action<Texture> f)
		{
			f(SuperController.singleton.fileBrowserUI.folderIcon.texture);
		}

		public static Texture GetFileIconFromCache(string path)
		{
			var imgPath = GetThumbnailPath(path);

			if (imgPath == null)
				return SuperController.singleton.fileBrowserUI.GetFileIcon(path)?.texture;
			else
				return ImageLoaderThreaded.singleton.GetCachedThumbnail(imgPath);
		}

		public static void GetFileIcon(string path, Action<Texture> f)
		{
			var t = GetFileIconFromCache(path);
			if (t != null)
			{
				f(t);
				return;
			}

			var q = new ImageLoaderThreaded.QueuedImage
			{
				imgPath = GetThumbnailPath(path),
				callback = tt => f(tt?.tex)
			};

			ImageLoaderThreaded.singleton.QueueThumbnail(q);
		}

		private static string GetThumbnailPath(string file)
		{
			var exts = new string[] { ".jpg", ".JPG" };

			foreach (var e in exts)
			{
				var relImgPath = Path.Parent(file) + "\\" + Path.Stem(file) + e;
				var imgPath = FileManagerSecure.GetFullPath(relImgPath);

				if (FileManagerSecure.FileExists(imgPath))
					return imgPath;
			}

			return null;
		}
	}
}
