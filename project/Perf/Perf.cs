// See https://aka.ms/new-console-template for more information

namespace AUI;

class PerfTest
{
	public static void Main()
	{
		new PerfTest().Run();
	}

	public void Run()
	{
		var sys = new VFSSys();

		const int PackageCount = 20000;
		const int DirCount = 5;
		const int FileCount = 5;

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

		RunPerf();
	}

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


	private void RunPerf()
	{
		var fs = FS.Filesystem.Instance;

		//fs.Pin("VaM/Custom/Assets");

		var cx = new FS.Context(
			null, null, "Custom/Assets",
			FS.Context.NoSort, FS.Context.SortAscending,
			FS.Context.RecursiveFlag |
			FS.Context.MergePackagesFlag |
			FS.Context.LatestPackagesOnlyFlag,
			null, null, new FS.Whitelist(new string[] { "VaM/Custom/Assets" }));

		var o = FS.Filesystem.Instance.Resolve(cx, "VaM/Custom/Assets") as FS.IFilesystemContainer;

		var files = o.GetFiles(cx);

		Console.WriteLine($"{files.Count}");
	}
}

