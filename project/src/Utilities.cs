using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
using UnityEngine;

namespace AUI
{
	static class Bits
	{
		public static bool IsSet(int flag, int bits)
		{
			return ((flag & bits) == bits);
		}

		public static bool IsAnySet(int flag, int bits)
		{
			return ((flag & bits) != 0);
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

		public static int GetHashCode<T1, T2, T3, T4, T5, T6, T7>(
			T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
		{
			unchecked
			{
				int hash = arg1.GetHashCode();
				hash = 31 * hash + arg2.GetHashCode();
				hash = 31 * hash + arg3.GetHashCode();
				hash = 31 * hash + arg4.GetHashCode();
				hash = 31 * hash + arg5.GetHashCode();
				hash = 31 * hash + arg6.GetHashCode();
				return 31 * hash + arg7.GetHashCode();
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

		public static void TimeThis(string s, Action f)
		{
			var sw = new Stopwatch();
			sw.Reset();
			sw.Start();
			f();
			sw.Stop();
			var ms = ((((double)sw.ElapsedTicks) / Stopwatch.Frequency) * 1000);
			Log.Info($"{s} {ms:0.000}ms");
		}

		public static void NatSort(List<string> list)
		{
			list.Sort(new NaturalStringComparer());
		}

		public static void NatSort<T>(List<T> list)
		{
			list.Sort(new GenericNaturalStringComparer<T>());
		}

		public static int CompareNatural(string strA, string strB)
		{
			return CompareNatural(strA, strB, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);
		}

		// from https://stackoverflow.com/questions/248603
		//
		public static int CompareNatural(string strA, string strB, CultureInfo culture, CompareOptions options)
		{
			CompareInfo cmp = culture.CompareInfo;
			int iA = 0;
			int iB = 0;
			int softResult = 0;
			int softResultWeight = 0;
			while (iA < strA.Length && iB < strB.Length)
			{
				bool isDigitA = Char.IsDigit(strA[iA]);
				bool isDigitB = Char.IsDigit(strB[iB]);
				if (isDigitA != isDigitB)
				{
					return cmp.Compare(strA, iA, strB, iB, options);
				}
				else if (!isDigitA && !isDigitB)
				{
					int jA = iA + 1;
					int jB = iB + 1;
					while (jA < strA.Length && !Char.IsDigit(strA[jA])) jA++;
					while (jB < strB.Length && !Char.IsDigit(strB[jB])) jB++;
					int cmpResult = cmp.Compare(strA, iA, jA - iA, strB, iB, jB - iB, options);
					if (cmpResult != 0)
					{
						// Certain strings may be considered different due to "soft" differences that are
						// ignored if more significant differences follow, e.g. a hyphen only affects the
						// comparison if no other differences follow
						string sectionA = strA.Substring(iA, jA - iA);
						string sectionB = strB.Substring(iB, jB - iB);
						if (cmp.Compare(sectionA + "1", sectionB + "2", options) ==
							cmp.Compare(sectionA + "2", sectionB + "1", options))
						{
							return cmp.Compare(strA, iA, strB, iB, options);
						}
						else if (softResultWeight < 1)
						{
							softResult = cmpResult;
							softResultWeight = 1;
						}
					}
					iA = jA;
					iB = jB;
				}
				else
				{
					char zeroA = (char)(strA[iA] - (int)Char.GetNumericValue(strA[iA]));
					char zeroB = (char)(strB[iB] - (int)Char.GetNumericValue(strB[iB]));
					int jA = iA;
					int jB = iB;
					while (jA < strA.Length && strA[jA] == zeroA) jA++;
					while (jB < strB.Length && strB[jB] == zeroB) jB++;
					int resultIfSameLength = 0;
					do
					{
						isDigitA = jA < strA.Length && Char.IsDigit(strA[jA]);
						isDigitB = jB < strB.Length && Char.IsDigit(strB[jB]);
						int numA = isDigitA ? (int)Char.GetNumericValue(strA[jA]) : 0;
						int numB = isDigitB ? (int)Char.GetNumericValue(strB[jB]) : 0;
						if (isDigitA && (char)(strA[jA] - numA) != zeroA) isDigitA = false;
						if (isDigitB && (char)(strB[jB] - numB) != zeroB) isDigitB = false;
						if (isDigitA && isDigitB)
						{
							if (numA != numB && resultIfSameLength == 0)
							{
								resultIfSameLength = numA < numB ? -1 : 1;
							}
							jA++;
							jB++;
						}
					}
					while (isDigitA && isDigitB);
					if (isDigitA != isDigitB)
					{
						// One number has more digits than the other (ignoring leading zeros) - the longer
						// number must be larger
						return isDigitA ? 1 : -1;
					}
					else if (resultIfSameLength != 0)
					{
						// Both numbers are the same length (ignoring leading zeros) and at least one of
						// the digits differed - the first difference determines the result
						return resultIfSameLength;
					}
					int lA = jA - iA;
					int lB = jB - iB;
					if (lA != lB)
					{
						// Both numbers are equivalent but one has more leading zeros
						return lA > lB ? -1 : 1;
					}
					else if (zeroA != zeroB && softResultWeight < 2)
					{
						softResult = cmp.Compare(strA, iA, 1, strB, iB, 1, options);
						softResultWeight = 2;
					}
					iA = jA;
					iB = jB;
				}
			}
			if (iA < strA.Length || iB < strB.Length)
			{
				return iA < strA.Length ? 1 : -1;
			}
			else if (softResult != 0)
			{
				return softResult;
			}
			return 0;
		}
	}


	public class NaturalStringComparer : IComparer<string>
	{
		public int Compare(string x, string y)
		{
			return U.CompareNatural(x, y);
		}
	}


	public class GenericNaturalStringComparer<T> : IComparer<T>
	{
		private static readonly Regex _re =
			new Regex(@"(?<=\D)(?=\d)|(?<=\d)(?=\D)", RegexOptions.Compiled);

		public int Compare(T xt, T yt)
		{
			string x = xt.ToString();
			string y = yt.ToString();
			return U.CompareNatural(x, y);
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
