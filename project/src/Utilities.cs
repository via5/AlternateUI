using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AUI
{
	static class Bits
	{
		public static bool IsSet(int flag, int bits)
		{
			return ((flag & bits) == bits);
		}

		public static int Bit(int pos)
		{
			return (1 << pos);
		}
	}


	static class Strings
	{
		public static string Get(string s, params object[] ps)
		{
			if (ps.Length > 0)
				return string.Format(s, ps);
			else
				return s;
		}
	}


	static class HashHelper
	{
		public static int GetHashCode<T1, T2>(T1 arg1, T2 arg2)
		{
			unchecked
			{
				return 31 * arg1.GetHashCode() + arg2.GetHashCode();
			}
		}

		public static int GetHashCode<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
		{
			unchecked
			{
				int hash = arg1.GetHashCode();
				hash = 31 * hash + arg2.GetHashCode();
				return 31 * hash + arg3.GetHashCode();
			}
		}

		public static int GetHashCode<T1, T2, T3, T4>(T1 arg1, T2 arg2, T3 arg3,
			T4 arg4)
		{
			unchecked
			{
				int hash = arg1.GetHashCode();
				hash = 31 * hash + arg2.GetHashCode();
				hash = 31 * hash + arg3.GetHashCode();
				return 31 * hash + arg4.GetHashCode();
			}
		}
	}


	class U
	{
		private static float lastErrorTime_ = 0;
		private static int errorCount_ = 0;
		private const int MaxErrors = 3;

		public static void Safe(Action a)
		{
			try
			{
				a();
			}
			catch (Exception e)
			{
				Log.Error(e.ToString());

				var now = Time.realtimeSinceStartup;

				if (now - lastErrorTime_ < 1)
				{
					++errorCount_;
					if (errorCount_ > MaxErrors)
					{
						Log.Error(
							$"more than {MaxErrors} errors in the last " +
							"second, disabling plugin");

						AlternateUI.Instance.DisablePlugin();
					}
				}
				else
				{
					errorCount_ = 0;
				}

				lastErrorTime_ = now;
			}
		}

		public static T Clamp<T>(T val, T min, T max)
			where T : IComparable<T>
		{
			if (val.CompareTo(min) < 0)
				return min;
			else if (val.CompareTo(max) > 0)
				return max;
			else
				return val;
		}

		public static void NatSort(List<string> list)
		{
			list.Sort(new NaturalStringComparer());
		}

		public static void NatSort<T>(List<T> list)
		{
			list.Sort(new GenericNaturalStringComparer<T>());
		}
	}


	// from https://stackoverflow.com/questions/248603
	public class NaturalStringComparer : IComparer<string>
	{
		private static readonly Regex _re =
			new Regex(@"(?<=\D)(?=\d)|(?<=\d)(?=\D)", RegexOptions.Compiled);

		public int Compare(string x, string y)
		{
			x = x.ToLower();
			y = y.ToLower();
			if (string.Compare(x, 0, y, 0, Math.Min(x.Length, y.Length)) == 0)
			{
				if (x.Length == y.Length) return 0;
				return x.Length < y.Length ? -1 : 1;
			}
			var a = _re.Split(x);
			var b = _re.Split(y);
			int i = 0;
			while (true)
			{
				int r = PartCompare(a[i], b[i]);
				if (r != 0)
					return r;

				if (a[i] != b[i])
					return string.Compare(a[i], b[i]);

				++i;
			}
		}

		private static int PartCompare(string x, string y)
		{
			int a, b;
			if (int.TryParse(x, out a) && int.TryParse(y, out b))
				return a.CompareTo(b);
			return x.CompareTo(y);
		}
	}


	// from https://stackoverflow.com/questions/248603
	public class GenericNaturalStringComparer<T> : IComparer<T>
	{
		private static readonly Regex _re =
			new Regex(@"(?<=\D)(?=\d)|(?<=\d)(?=\D)", RegexOptions.Compiled);

		public int Compare(T xt, T yt)
		{
			string x = xt.ToString();
			string y = yt.ToString();

			x = x.ToLower();
			y = y.ToLower();
			if (string.Compare(x, 0, y, 0, Math.Min(x.Length, y.Length)) == 0)
			{
				if (x.Length == y.Length) return 0;
				return x.Length < y.Length ? -1 : 1;
			}
			var a = _re.Split(x);
			var b = _re.Split(y);
			int i = 0;
			while (true)
			{
				int r = PartCompare(a[i], b[i]);
				if (r != 0)
					return r;

				if (a[i] != b[i])
					return string.Compare(a[i], b[i]);

				++i;
			}
		}

		private static int PartCompare(string x, string y)
		{
			int a, b;
			if (int.TryParse(x, out a) && int.TryParse(y, out b))
				return a.CompareTo(b);
			return x.CompareTo(y);
		}
	}


	class Ticker
	{
		private readonly string name_;
		private Stopwatch w_ = new Stopwatch();
		private long freq_ = Stopwatch.Frequency;
		private long ticks_ = 0;
		private long calls_ = 0;

		private float elapsed_ = 0;
		private long avg_ = 0;
		private long peak_ = 0;

		private long lastPeak_ = 0;
		private long lastCalls_ = 0;
		private bool updated_ = false;

		public Ticker(string name = "")
		{
			name_ = name;
		}

		public string Name
		{
			get { return name_; }
		}

		public void Do(Action f)
		{
			updated_ = false;

			w_.Reset();
			w_.Start();
			f();
			w_.Stop();

			++calls_;
			ticks_ += w_.ElapsedTicks;
			peak_ = Math.Max(peak_, w_.ElapsedTicks);
		}

		public void Update(float s)
		{
			elapsed_ += s;
			if (elapsed_ >= 1)
			{
				if (calls_ <= 0)
					avg_ = 0;
				else
					avg_ = ticks_ / calls_;

				lastPeak_ = peak_;
				lastCalls_ = calls_;

				ticks_ = 0;
				calls_ = 0;
				elapsed_ = 0;
				peak_ = 0;
				updated_ = true;
			}
		}

		public bool Updated
		{
			get { return updated_; }
		}

		public float AverageMs
		{
			get
			{
				return ToMs(avg_);
			}
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

		public override string ToString()
		{
			return $"calls={Calls} avg={AverageMs:0.000} peak={PeakMS:0.000}";
		}
	}


	public class IgnoreFlag
	{
		private bool ignore_ = false;

		public static implicit operator bool(IgnoreFlag f)
		{
			return f.ignore_;
		}

		public void Do(Action a)
		{
			try
			{
				ignore_ = true;
				a();
			}
			finally
			{
				ignore_ = false;
			}
		}
	}


	static class Log
	{
		public const int ErrorLevel = 0;
		public const int WarningLevel = 1;
		public const int InfoLevel = 2;
		public const int VerboseLevel = 3;

		public static string LevelToShortString(int i)
		{
			switch (i)
			{
				case ErrorLevel: return "E";
				case WarningLevel: return "W";
				case InfoLevel: return "I";
				case VerboseLevel: return "V";
				default: return $"?{i}";
			}
		}

		public static void Out(int level, string s)
		{
			var t = DateTime.Now.ToString("hh:mm:ss.fff");
			string p = LevelToShortString(level);

			if (level == ErrorLevel)
				SuperController.LogError($"{t} !![{p}] {s}");
			else
				SuperController.LogError($"{t}   [{p}] {s}");
		}

		public static void Verbose(string s)
		{
			//Out(VerboseLevel, s);
		}

		public static void Info(string s)
		{
			Out(InfoLevel, s);
		}

		public static void Warning(string s)
		{
			Out(WarningLevel, s);
		}

		public static void Error(string s)
		{
			Out(ErrorLevel, s);
		}

		public static void ErrorST(string s)
		{
			Out(ErrorLevel, $"{s}\n{new StackTrace(1)}");
		}
	}


	class Logger
	{
		public const int All = int.MaxValue;
		private static int enabled_ = All;

		private int type_;
		private Func<string> prefix_;

		public Logger(int type, string prefix)
		{
			type_ = type;
			prefix_ = () => prefix;
		}

		public Logger(int type, Func<string> prefix)
		{
			type_ = type;
			prefix_ = prefix;
		}

		public Logger(int type, Atom a, string prefix)
		{
			type_ = type;
			prefix_ = () => a.uid + (prefix == "" ? "" : " " + prefix);
		}

		public static int Enabled
		{
			get { return enabled_; }
			set { enabled_ = value; }
		}

		public string Prefix
		{
			get { return prefix_(); }
		}

		public void Verbose(string s)
		{
			if (IsEnabled())
				Log.Verbose($"{Prefix}: {s}");
		}

		public void Info(string s)
		{
			if (IsEnabled())
				Log.Info($"{Prefix}: {s}");
		}

		public void Warning(string s)
		{
			Log.Warning($"{Prefix}: {s}");
		}

		public void Error(string s)
		{
			Log.Error($"{Prefix}: {s}");
		}

		public void ErrorST(string s)
		{
			Log.ErrorST($"{Prefix}: {s}\n{new StackTrace(1)}");
		}

		private bool IsEnabled()
		{
			return Bits.IsSet(enabled_, type_);
		}
	}
}
