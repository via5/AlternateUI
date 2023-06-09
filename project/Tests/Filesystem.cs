using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace AUI.Tests
{
	[TestClass]
	public class Filesystem
	{
		private void Create(ISys sys = null)
		{
			if (sys == null)
			{
				string data = "c:\\tmp\\aui-tests";
				sys = new FSSys(data + "\\root", data + "\\packages");
			}

			new MockAlternateUIScript(sys);
			FS.Filesystem.Init();
		}


		[TestMethod]
		public void Merged()
		{
			string[] expected = new string[]
			{
				"VaM/Packages/MeshedVR.AssetsPack.1/Custom/Assets/MeshedVR/AssetsPack/bathroom_shower.assetbundle",
				"VaM/Custom/Assets/MeshedVR/car_sedan.assetbundle",
				"VaM/Packages/MeshedVR.AssetsPack.1/Custom/Assets/MeshedVR/AssetsPack/car_sedan.assetbundle",
				"VaM/Packages/Xstatic.MegaParticlePack.1/Custom/Assets/Xstatic/firefly particles.assetbundle",
				"VaM/Packages/Xstatic.MegaParticlePack.1/Custom/Assets/Xstatic/flames.assetbundle",
				"VaM/Packages/Xstatic.MegaParticlePack.1/Custom/Assets/Xstatic/Xstatic particle effects pack#1.assetbundle",
				"VaM/Packages/Xstatic.MegaParticlePack.1/Custom/Assets/Xstatic/Xstatic particle effects pack#2.assetbundle",
				"VaM/Packages/Xstatic.MegaParticlePack.1/Custom/Assets/Xstatic/Xstatic particle effects pack#3.assetbundle",
				"VaM/Packages/Xstatic.MegaParticlePack.1/Custom/Assets/Xstatic/Xstatic particle pack#3.assetbundle",
				"VaM/Packages/Xstatic.MegaParticlePack.1/Custom/Assets/Xstatic/Xstatic particle pack#6.assetbundle",
				"VaM/Packages/Xstatic.MegaParticlePack.1/Custom/Assets/Xstatic/Xstatic particle pack#7 fireworks.assetbundle",
			};

			Create();

			var cx = new FS.Context(
				null, null, "Custom/Assets",
				FS.Context.SortFilename, FS.Context.SortAscending,
				FS.Context.RecursiveFlag |
				FS.Context.MergePackagesFlag |
				FS.Context.LatestPackagesOnlyFlag,
				null, null, null);

			var d = FS.Filesystem.Instance.Resolve(cx, "VaM/Custom/Assets")
				as FS.IFilesystemContainer;

			FS.Filesystem.Instance.ClearCaches();

			var got = new List<string>();
			foreach (var f in d.GetFiles(cx))
				got.Add(f.VirtualPath);

			var actual = got.ToArray();

			Assert.AreEqual(expected.Length, actual.Length);
			for (int i = 0; i < expected.Length; ++i)
				Assert.AreEqual(expected[i], actual[i]);
		}

		[TestMethod]
		public void DotFiles()
		{
			Create();

			string[] expectedHidden = new string[]
			{
				"VaM/Custom/Scripts/TestScript/folder",
				"VaM/Custom/Scripts/TestScript/file"
			};

			string[] expectedVisible = new string[]
			{
				"VaM/Custom/Scripts/TestScript/.dotfolder",
				"VaM/Custom/Scripts/TestScript/folder",
				"VaM/Custom/Scripts/TestScript/.dotfile",
				"VaM/Custom/Scripts/TestScript/file"
			};

			{
				var cx = new FS.Context(
					null, null, "Custom/Scripts",
					FS.Context.SortFilename, FS.Context.SortAscending,
					FS.Context.NoFlags,
					null, null, null);

				var d = FS.Filesystem.Instance.Resolve(cx, "VaM/Custom/Scripts/TestScript")
					as FS.IFilesystemContainer;

				var got = new List<string>();

				foreach (var f in d.GetDirectories(cx))
					got.Add(f.VirtualPath);

				foreach (var f in d.GetFiles(cx))
					got.Add(f.VirtualPath);

				var actual = got.ToArray();

				Assert.AreEqual(expectedHidden.Length, actual.Length);
				for (int i = 0; i < expectedHidden.Length; ++i)
					Assert.AreEqual(expectedHidden[i], actual[i]);
			}

			{
				var cx = new FS.Context(
					null, null, "Custom/Scripts",
					FS.Context.SortFilename, FS.Context.SortAscending,
					FS.Context.ShowHiddenFilesFlag | FS.Context.ShowHiddenFoldersFlag,
					null, null, null);

				var d = FS.Filesystem.Instance.Resolve(cx, "VaM/Custom/Scripts/TestScript")
					as FS.IFilesystemContainer;

				var got = new List<string>();

				foreach (var f in d.GetDirectories(cx))
					got.Add(f.VirtualPath);

				foreach (var f in d.GetFiles(cx))
					got.Add(f.VirtualPath);

				var actual = got.ToArray();

				Assert.AreEqual(expectedVisible.Length, actual.Length);
				for (int i = 0; i < expectedVisible.Length; ++i)
					Assert.AreEqual(expectedVisible[i], actual[i]);
			}
		}

		[TestMethod]
		public void Perf()
		{
			var sys = new VFSSys();

			const int PackageCount = 7000;
			const int DirCount = 2;
			const int FileCount = 2;

			for (int i = 0; i < PackageCount; ++i)
			{
				var p = new VFSSys.Package();
				p.name = $"package{i}";

				for (int d = 0; d < DirCount; ++d)
				{
					p.dirs.Add($"Custom/Assets/dir{d}/");

					for (int f = 0; f < FileCount; ++f)
						p.files.Add($"Custom/Assets/dir{d}/package{i}-file{f}");
				}

				sys.AddPackage(p);
			}

			sys.AddDir("Custom/");
			sys.AddDir("Custom/Assets/");

			for (int d = 0; d < DirCount; ++d)
				sys.AddDir($"Custom/Assets/dir{d}/");

			for (int f = 0; f < FileCount; ++f)
				sys.AddFile($"Custom/Assets/file{f}");

			Create(sys);

			var cx = new FS.Context(
				null, null, "Custom/Assets",
				FS.Context.SortFilename, FS.Context.SortAscending,
				FS.Context.RecursiveFlag |
				FS.Context.MergePackagesFlag |
				FS.Context.LatestPackagesOnlyFlag,
				null, null, null);

			var o = FS.Filesystem.Instance.Resolve(cx, "VaM/Custom/Assets") as FS.IFilesystemContainer;
			var fs = o.GetFiles(cx);

			Console.WriteLine($"{fs.Count}");
		}

		static void Main()
		{
			new Filesystem().Perf();
		}
	}
}
