using System.Diagnostics;
using System;

namespace AUI.FS
{
	class Ticker
	{
		private readonly string name_;
		private Stopwatch w_ = new Stopwatch();
		private long freq_ = Stopwatch.Frequency;
		private long ticks_ = 0;
		private long calls_ = 0;
		private long gcStart_ = 0;
		private long gc_ = 0;

		private long avg_ = 0;
		private long peak_ = 0;

		private long lastTotal_ = 0;
		private long lastPeak_ = 0;
		private long lastCalls_ = 0;
		private long lastGc_ = 0;
		private long gcTotal_ = 0;
		private bool updated_ = false;

		public Ticker(string name = "")
		{
			name_ = name;
		}

		public string Name
		{
			get { return name_; }
		}

		public void Start()
		{
			updated_ = false;

			gcStart_ = GC.GetTotalMemory(false);

			w_.Reset();
			w_.Start();
		}

		public void End()
		{
			w_.Stop();

			++calls_;
			ticks_ += w_.ElapsedTicks;
			peak_ = Math.Max(peak_, w_.ElapsedTicks);

			gc_ += GC.GetTotalMemory(false) - gcStart_;
		}

		public void Update(float s)
		{
			if (calls_ <= 0)
				avg_ = 0;
			else
				avg_ = ticks_ / calls_;

			lastTotal_ = ticks_;
			lastPeak_ = peak_;
			lastCalls_ = calls_;
			lastGc_ = gc_;

			if (gc_ > 0)
				gcTotal_ += gc_;

			ticks_ = 0;
			calls_ = 0;
			peak_ = 0;
			gc_ = 0;
			updated_ = true;
		}

		public bool Updated
		{
			get { return updated_; }
		}

		public float TotalMs
		{
			get { return ToMs(lastTotal_); }
		}

		public float AverageMs
		{
			get { return ToMs(avg_); }
		}

		public float PeakMS
		{
			get { return ToMs(lastPeak_); }
		}

		private float ToMs(long ticks)
		{
			return (float)((((double)ticks) / freq_) * 1000);
		}

		public long Calls
		{
			get { return lastCalls_; }
		}

		public long MemoryChange
		{
			get { return lastGc_; }
		}

		public override string ToString()
		{
			return
				$"n={Calls,7} " +
				$"tot={TotalMs,8:####0.00} " +
				$"avg={AverageMs,8:####0.00} " +
				$"peak={PeakMS,8:####0.00} " +
				$"gc={BytesToString(MemoryChange)} " +
				$"gct={BytesToString(gcTotal_)}";
		}

		private string BytesToString(long bytes)
		{
			string[] sizes = { "B", "K", "M", "G", "T" };
			double len = (double)Math.Abs(bytes);
			int order = 0;
			while (len >= 1024 && order < sizes.Length - 1)
			{
				order++;
				len = len / 1024;
			}

			// Adjust the format string to your preferences. For example "{0:0.#}{1}" would
			// show a single decimal place, and no space.
			string s = string.Format($"{len,6:###0.0} {sizes[order]}");

			if (bytes < 0)
				s = "-" + s;

			return s;
		}
	}


	struct InstrumentationType
	{
		private readonly string name_;
		private readonly int v_;

		public InstrumentationType(string name, int v)
		{
			name_ = name;
			v_ = v;
		}

		public int Int
		{
			get { return v_; }
		}

		public override string ToString()
		{
			return name_;
		}

		public static InstrumentationType[] Values
		{
			get
			{
				return new InstrumentationType[I.Count]
				{
					I.FDGetFiles,
					I.FDSetFiles,
					I.FDTreeSetFlags,
					I.FDTreeRefresh,
					I.FDTreeSelect,
					I.FDSetPath,
					I.FDUpdateButton,
					I.FTIGetDirectories,
					I.FTIHasDirectories,
					I.FTICreateItem,

					I.FMSGetDirectories,
					I.FMSGetFiles,
					I.FMSGetShortCutsForDirectory,
					I.FMSFileCreationTime,
					I.FMSFileLastWriteTime,
					I.FMSDirectoryCreationTime,
					I.FMSDirectoryLastWriteTime,
					I.FMSIsDirectoryInPackage,
					I.FMSFileExists,

					I.RefreshPackages,
					I.CallingDoGetDirectories,
					I.CheckDirectories,
					I.CallingDoGetFiles,
					I.CheckFiles,
					I.BasicDoGetDirectories,
					I.BasicDoGetFiles,
					I.CallingDoHasDirectories,
					I.BasicDoHasDirectories,

					I.MergePackages,
					I.MergePackagesStart,
					I.MergePackagesGetPackages,
					I.MergePackagesResolve,
					I.MergePackagesAdd,

					I.Resolve,
					I.PackageResolveInternal,
					I.ResolveInternalInDirectories,
					I.ResolveInternalInFiles,

					I.IsPinned,
					I.Unpin,
					I.Pin,
					I.PinSave,
					I.FirePinsChanged,
					I.FireObjectChanged,
					I.FTFindItem,
					I.FTRefreshOnObjectchanged,

					I.FTIGetFSObject,
					I.RefreshShortCuts,

					I.PackageResolveInternal1,
					I.PackageResolveInternal2,
					I.PackageResolveInternal3,
					I.BasicDirectoryResolve,

					I.IncludeDirectoryGetRVP,
					I.IncludeDirectoryWhitelistMatches,

					I.SortDirectories,
					I.UpdateLookup,
					I.SetLocalDirectoriesCache
				};
			}
		}
	}

	static class I
	{
		public static InstrumentationType FDGetFiles = new InstrumentationType("FDGetFiles", 0);
		public static InstrumentationType FDSetFiles = new InstrumentationType("FDSetFiles", 1);
		public static InstrumentationType FDTreeSetFlags = new InstrumentationType("FDTreeSetFlags", 2);
		public static InstrumentationType FDTreeRefresh = new InstrumentationType("FDTreeRefresh", 3);
		public static InstrumentationType FDTreeSelect = new InstrumentationType("FDTreeSelect", 4);
		public static InstrumentationType FDSetPath = new InstrumentationType("FDSetPath", 5);
		public static InstrumentationType FDUpdateButton = new InstrumentationType("FDUpdateButton", 6);
		public static InstrumentationType FTIGetDirectories = new InstrumentationType("FTIGetSubDirectories", 7);
		public static InstrumentationType FTIHasDirectories = new InstrumentationType("FTIHasSubDirectories", 8);
		public static InstrumentationType FTICreateItem = new InstrumentationType("FTICreateItem", 9);

		public static InstrumentationType FMSGetDirectories = new InstrumentationType("FMSGetDirectories", 10);
		public static InstrumentationType FMSGetFiles = new InstrumentationType("FMSGetFiles", 11);
		public static InstrumentationType FMSGetShortCutsForDirectory = new InstrumentationType("FMSGetShortCutsForDirectory", 12);
		public static InstrumentationType FMSFileCreationTime = new InstrumentationType("FMSFileCreationTime", 13);
		public static InstrumentationType FMSFileLastWriteTime = new InstrumentationType("FMSFileLastWriteTime", 14);
		public static InstrumentationType FMSDirectoryCreationTime = new InstrumentationType("FMSDirectoryCreationTime", 15);
		public static InstrumentationType FMSDirectoryLastWriteTime = new InstrumentationType("FMSDirectoryLastWriteTime", 16);
		public static InstrumentationType FMSIsDirectoryInPackage = new InstrumentationType("FMSIsDirectoryInPackage", 17);
		public static InstrumentationType FMSFileExists = new InstrumentationType("FMSFileExists", 18);

		public static InstrumentationType RefreshPackages = new InstrumentationType("RefreshPackages", 19);
		public static InstrumentationType CallingDoGetDirectories = new InstrumentationType("CallingDoGetDirectories", 20);
		public static InstrumentationType CheckDirectories = new InstrumentationType("CheckDirectories", 21);
		public static InstrumentationType CallingDoGetFiles = new InstrumentationType("CallingDoGetFiles", 22);
		public static InstrumentationType CheckFiles = new InstrumentationType("CheckFiles", 23);
		public static InstrumentationType BasicDoGetDirectories = new InstrumentationType("BasicDoGetDirectories", 24);
		public static InstrumentationType BasicDoGetFiles = new InstrumentationType("BasicDoGetFiles", 25);
		public static InstrumentationType CallingDoHasDirectories = new InstrumentationType("CallingDoHasDirectories", 26);
		public static InstrumentationType BasicDoHasDirectories = new InstrumentationType("BasicDoHasDirectories", 27);

		public static InstrumentationType MergePackages = new InstrumentationType("MergePackages", 28);
		public static InstrumentationType MergePackagesStart = new InstrumentationType("MergePackagesStart", 29);
		public static InstrumentationType MergePackagesGetPackages = new InstrumentationType("MergePackagesGetPackages", 30);
		public static InstrumentationType MergePackagesResolve = new InstrumentationType("MergePackagesResolve", 31);
		public static InstrumentationType MergePackagesAdd = new InstrumentationType("MergePackagesAdd", 32);

		public static InstrumentationType Resolve = new InstrumentationType("Resolve", 33);
		public static InstrumentationType PackageResolveInternal = new InstrumentationType("PackageResolveInternal", 34);
		public static InstrumentationType ResolveInternalInDirectories = new InstrumentationType("ResolveInternalInDirectories", 35);
		public static InstrumentationType ResolveInternalInFiles = new InstrumentationType("ResolveInternalInFiles", 36);

		public static InstrumentationType IsPinned = new InstrumentationType("IsPinned", 37);
		public static InstrumentationType Unpin = new InstrumentationType("Unpin", 38);
		public static InstrumentationType Pin = new InstrumentationType("Pin", 39);
		public static InstrumentationType PinSave = new InstrumentationType("PinSave", 40);
		public static InstrumentationType FirePinsChanged = new InstrumentationType("FirePinsChanged", 41);
		public static InstrumentationType FireObjectChanged = new InstrumentationType("FireObjectChanged", 42);
		public static InstrumentationType FTFindItem = new InstrumentationType("FTFindItem", 43);
		public static InstrumentationType FTRefreshOnObjectchanged = new InstrumentationType("FTRefreshOnObjectchanged", 44);

		public static InstrumentationType FTIGetFSObject = new InstrumentationType("FTIGetFSObject", 45);
		public static InstrumentationType RefreshShortCuts = new InstrumentationType("RefreshShortCuts", 46);

		public static InstrumentationType PackageResolveInternal1 = new InstrumentationType("PackageResolveInternal1", 47);
		public static InstrumentationType PackageResolveInternal2 = new InstrumentationType("PackageResolveInternal2", 48);
		public static InstrumentationType PackageResolveInternal3 = new InstrumentationType("PackageResolveInternal3", 49);

		public static InstrumentationType BasicDirectoryResolve = new InstrumentationType("BasicDirectoryResolve", 50);
		public static InstrumentationType IncludeDirectoryGetRVP = new InstrumentationType("IncludeDirectoryGetRVP", 51);
		public static InstrumentationType IncludeDirectoryWhitelistMatches = new InstrumentationType("IncludeDirectoryWhitelistMatches", 52);

		public static InstrumentationType SortDirectories = new InstrumentationType("SortDirectories", 53);
		public static InstrumentationType UpdateLookup = new InstrumentationType("UpdateLookup", 54);
		public static InstrumentationType SetLocalDirectoriesCache = new InstrumentationType("SetDirsCache", 55);

		public const int Count = 56;
	}



	class Instrumentation
	{
		public const bool AlwaysActive = false;

		private Ticker[] tickers_ = new Ticker[I.Count];
		private int[] depth_ = new int[I.Count]
		{
			0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 1,
			1,
				2, 2, 2, 2,
			1,
				2, 2, 2,
			1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
			1, 1, 1, 1, 1, 1, 1, 1, 1
		};

		private int[] stack_ = new int[30];
		private int current_ = 0;
		private bool enabled_ = false;
		private bool started_ = false;
		private static Instrumentation instance_ = new Instrumentation();


		public Instrumentation()
		{
			instance_ = this;

			foreach (var i in InstrumentationType.Values)
				tickers_[i.Int] = new Ticker(i.ToString());
		}

		public static Instrumentation Instance
		{
			get { return instance_; }
		}

		public bool Updated
		{
			get { return started_; }
		}

		public bool Enabled
		{
			get { return enabled_ || AlwaysActive; }
			set { enabled_ = value; }
		}

		public static void Reset()
		{
			instance_.DoReset();
		}

		public static void Start(InstrumentationType i)
		{
			instance_.DoStart(i);
		}

		public static void End()
		{
			instance_.DoEnd();
		}

		public void UpdateTickers(float s)
		{
			if (!Enabled)
				return;

			for (int i = 0; i < tickers_.Length; ++i)
				tickers_[i].Update(s);
		}

		private void DoStart(InstrumentationType i)
		{
			if (!Enabled)
				return;

			if (current_ < 0 || current_ >= stack_.Length)
			{
				AlternateUI.Instance.Log.ErrorST($"bad current {current_}");
				AlternateUI.Instance.DisablePlugin();
			}

			if (i.Int < 0 || i.Int >= tickers_.Length)
			{
				AlternateUI.Instance.Log.ErrorST($"bad index {i}");
				AlternateUI.Instance.DisablePlugin();
			}

			stack_[current_] = i.Int;
			++current_;
			started_ = true;

			tickers_[i.Int].Start();
		}

		private void DoEnd()
		{
			if (!Enabled)
				return;

			if (current_ == 0)
			{
				AlternateUI.Instance.Log.ErrorST($"bad current {current_}");
				AlternateUI.Instance.DisablePlugin();
			}

			--current_;

			int i = stack_[current_];
			stack_[current_] = -1;

			tickers_[i].End();
		}

		public void Dump(Logger log)
		{
			log.Info("times:");
			int longestLabel = 0;

			foreach (var i in InstrumentationType.Values)
			{
				string label = new string(' ', Depth(i)) + Name(i) + " ";
				longestLabel = Math.Max(longestLabel, label.Length);
			}

			foreach (var i in InstrumentationType.Values)
			{
				string label = new string(' ', Depth(i)) + Name(i) + " ";
				label = label.PadRight(longestLabel, ' ');

				log.Info($"{label}{Get(i)}");
			}
		}

		private void DoReset()
		{
			current_ = 0;
			started_ = false;
		}

		public int Depth(InstrumentationType i)
		{
			return depth_[i.Int];
		}

		public string Name(InstrumentationType i)
		{
			return tickers_[i.Int].Name;
		}

		public Ticker Get(InstrumentationType i)
		{
			return tickers_[i.Int];
		}
	}
}
