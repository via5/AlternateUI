using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;

namespace AUI.Tests
{
	[TestClass]
	public class Filesystem
	{
		[TestMethod]
		public void TestMethod1()
		{
			string data = new FileInfo("..\\..\\data").FullName;

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

			new MockScript(data + "\\root", data + "\\packages");
			FS.Filesystem.Init();

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
	}
}
