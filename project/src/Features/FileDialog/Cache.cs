using MVR.FileManagementSecure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AUI.FileDialog
{
	static class Cache
	{
		class CacheInfo
		{
			public string parent;
			public string normalizedParent;
			public string[] exts;

			public CacheInfo(string parent, string[] exts)
			{
				this.parent = parent;
				this.normalizedParent = parent.Replace('\\', '/');
				this.exts = exts;
			}

			public override int GetHashCode()
			{
				return HashHelper.Combine(
					normalizedParent.GetHashCode(),
					HashHelper.HashArray(exts));
			}

			public override bool Equals(object o)
			{
				return Equals(o as CacheInfo);
			}

			public bool Equals(CacheInfo o)
			{
				return
					normalizedParent == o.normalizedParent &&
					ArraysEqual(exts, o.exts);
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

		public static string[] SceneExtensions =
			new string[] { ".json", ".vac", ".vap", ".vam", ".scene" };

		private static readonly Dictionary<CacheInfo, List<File>> files_ =
			new Dictionary<CacheInfo, List<File>>();

		private static readonly Dictionary<CacheInfo, List<File>> dirs_ =
			new Dictionary<CacheInfo, List<File>>();

		private static readonly Dictionary<CacheInfo, List<ShortCut>> packages_ =
			new Dictionary<CacheInfo, List<ShortCut>>();

		private static List<File> packagesFlat_ = null;


		public static List<File> GetDirectories(string parent)
		{
			List<File> list;
			var ci = new CacheInfo(parent, null);

			if (!dirs_.TryGetValue(ci, out list))
			{
				list = new List<File>();
				foreach (var d in FileManagerSecure.GetDirectories(parent))
					list.Add(new File(d));

				U.NatSort(list);
				dirs_.Add(ci, list);
			}

			return list;
		}

		public static bool HasDirectories(string parent)
		{
			var list = GetDirectories(parent);
			return (list.Count > 0);
		}

		public static List<File> GetFiles(string parent, string[] exts)
		{
			List<File> list = null;
			var ci = new CacheInfo(parent, exts);

			double findTime = 0;
			double getTime = 0;
			double sortTime = 0;

			bool got = false;

			findTime = U.DebugTimeThis(() =>
			{
				got = files_.TryGetValue(ci, out list);
			});

			if (!got)
			{
				list = new List<File>();

				getTime = U.DebugTimeThis(() =>
				{
					foreach (var f in FileManagerSecure.GetFiles(parent))
					{
						if (exts == null)
						{
							list.Add(new File(f));
						}
						else
						{
							foreach (var e in exts)
							{
								if (f.EndsWith(e))
								{
									list.Add(new File(f));
									break;
								}
							}
						}
					}
				});

				sortTime = U.DebugTimeThis(() =>
				{
					U.NatSort(list);
				});

				files_.Add(ci, list);

				AlternateUI.Instance.Log.Info(
					$"cached {parent}: " +
					$"find={findTime:0.000}ms " +
					$"get={getTime:0.000}ms " +
					$"sort={sortTime:0.000}ms");
			}

			return list;
		}

		private static void GetFilesRecursive(string parent, string[] exts, List<File> list)
		{
			list.AddRange(GetFiles(parent, exts));

			foreach (var d in Cache.GetDirectories(parent))
				GetFilesRecursive(d.Path, exts, list);
		}

		public static bool HasPackages(string parent)
		{
			var scs = FileManagerSecure.GetShortCutsForDirectory("Saves/scene");
			return (scs != null && scs.Count > 0);
		}

		public static List<File> GetPackagesFlat(string[] exts)
		{
			if (packagesFlat_ == null)
			{
				packagesFlat_ = new List<File>();

				foreach (var p in FileManagerSecure.GetShortCutsForDirectory("Saves/scene"))
				{
					if (string.IsNullOrEmpty(p.package))
						continue;

					if (!string.IsNullOrEmpty(p.packageFilter))
						continue;

					GetFilesRecursive(p.path, exts, packagesFlat_);
				}
			}

			return packagesFlat_;
		}

		public static List<ShortCut> GetPackages(string parent)
		{
			var ci = new CacheInfo(parent, null);
			List<ShortCut> list;

			if (!packages_.TryGetValue(ci, out list))
			{
				list = FileManagerSecure.GetShortCutsForDirectory("Saves/scene");
				packages_.Add(ci, list);
			}

			return list;
		}
	}


	public static class Icons
	{
		private static Texture packageIcon_ = null;
		private static readonly List<Action<Texture>> packageIconCallbacks_ =
			new List<Action<Texture>>();

		public static void LoadAll()
		{
			ImageLoaderThreaded.QueuedImage queuedImage = new ImageLoaderThreaded.QueuedImage();
			queuedImage.imgPath = AlternateUI.Instance.PluginPath + "/res/package.png";
			queuedImage.linear = true;

			queuedImage.callback = (tt) =>
			{
				packageIcon_ = tt.tex;

				foreach (var f in packageIconCallbacks_)
					f(packageIcon_);

				packageIconCallbacks_.Clear();
			};

			//ImageLoaderThreaded.singleton.ClearCacheThumbnail(queuedImage.imgPath);
			ImageLoaderThreaded.singleton.QueueThumbnail(queuedImage);
		}

		public static void GetPackageIcon(Action<Texture> f)
		{
			if (packageIcon_ != null)
			{
				f(packageIcon_);
			}
			else
			{
				packageIconCallbacks_.Add(f);
			}
		}

		public static void GetDirectoryIcon(Action<Texture> f)
		{
			f(SuperController.singleton.fileBrowserUI.folderIcon.texture);
		}

		public static Texture GetFileIconFromCache(string path)
		{
			var imgPath = GetThumbnailPath(path);

			if (imgPath == null)
			{
				return SuperController.singleton.fileBrowserUI.GetFileIcon(path)?.texture;
			}
			else
			{
				//ImageLoaderThreaded.singleton.ClearCacheThumbnail(imgPath);
				return ImageLoaderThreaded.singleton.GetCachedThumbnail(imgPath);
			}
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
