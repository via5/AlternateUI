using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
using MVR.FileManagementSecure;

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
		public static int HashArray<T>(T[] array)
		{
			if (array == null)
				return 0;

			int hash = 17;
			for (int i = 0; i < array.Length; ++i)
				hash = hash * 31 + array[i].GetHashCode();

			return hash;
		}

		public static int GetHashCode<T1, T2>(T1 arg1, T2 arg2)
		{
			return Combine(arg1.GetHashCode(), arg2.GetHashCode());
		}

		public static int GetHashCode<T1, T2, T3>(T1 arg1, T2 arg2, T3 arg3)
		{
			return Combine(arg1.GetHashCode(), arg2.GetHashCode(), arg3.GetHashCode());
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

		public static int Combine(int arg1, int arg2)
		{
			unchecked
			{
				return 31 * arg1 + arg2;
			}
		}

		public static int Combine(int arg1, int arg2, int arg3)
		{
			unchecked
			{
				int hash = arg1;
				hash = 31 * hash + arg2;
				hash = 31 * hash + arg3;
				return hash;
			}
		}

		public static int Combine(int arg1, int arg2, int arg3, int arg4)
		{
			unchecked
			{
				int hash = arg1;
				hash = 31 * hash + arg2;
				hash = 31 * hash + arg3;
				hash = 31 * hash + arg4;
				return hash;
			}
		}

		public static int Combine(int arg1, int arg2, int arg3, int arg4, int arg5)
		{
			unchecked
			{
				int hash = arg1;
				hash = 31 * hash + arg2;
				hash = 31 * hash + arg3;
				hash = 31 * hash + arg4;
				hash = 31 * hash + arg5;
				return hash;
			}
		}
	}


	static class U
	{
		private static bool devMode_ = FileManagerSecure.FileExists(
			"Custom/PluginData/AlternateUI/devmode");

		public static bool DevMode
		{
			get { return devMode_; }
		}

		public static float Clamp(float val, float min, float max)
		{
			if (val < min)
				return min;
			else if (val > max)
				return max;
			else
				return val;
		}

		public static int Clamp(int val, int min, int max)
		{
			if (val < min)
				return min;
			else if (val > max)
				return max;
			else
				return val;
		}

		public static void DebugTimeThis(string s, Action f)
		{
			var sw = new Stopwatch();
			sw.Reset();
			sw.Start();
			f();
			sw.Stop();
			var ms = ((((double)sw.ElapsedTicks) / Stopwatch.Frequency) * 1000);
			AlternateUI.Instance.Log.Info($"{s} {ms:0.000}ms");
		}

		public static double DebugTimeThis(Action f)
		{
			var sw = new Stopwatch();
			sw.Reset();
			sw.Start();
			f();
			sw.Stop();
			var ms = ((((double)sw.ElapsedTicks) / Stopwatch.Frequency) * 1000);
			return ms;
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
						//string sectionA = strA.Substring(iA, jA - iA);
						//string sectionB = strB.Substring(iB, jB - iB);
						//if (cmp.Compare(sectionA + "1", sectionB + "2", options) ==
						//	cmp.Compare(sectionA + "2", sectionB + "1", options))
						{
							return cmp.Compare(strA, iA, strB, iB, options);
						}
						//else if (softResultWeight < 1)
						//{
						//	softResult = cmpResult;
						//	softResultWeight = 1;
						//}
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

		public static string PrettyFilename(string s)
		{
			var re = new Regex(@"(?:\w+\.(\w+)\.\d+:)?.*\/([^\/]*)(\.[^\.]+)?$");

			var m = re.Match(s);
			if (m == null)
				return null;

			string package = m.Groups[1]?.Value ?? "";
			string file = m.Groups[2]?.Value ?? "";

			if (string.IsNullOrEmpty(file))
				return s;

			string p;

			if (package == "")
				p = file;
			else
				p = package + ":" + file;

			return p;
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


	public class Logger
	{
		public const int ErrorLevel = 0;
		public const int WarningLevel = 1;
		public const int InfoLevel = 2;
		public const int VerboseLevel = 3;

		private Func<string> prefix_;
		private bool enabled_ = true;

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

		public Logger(string prefix)
		{
			prefix_ = () => prefix;
		}

		public Logger(Func<string> prefix)
		{
			prefix_ = prefix;
		}

		public Logger(Atom a, string prefix)
		{
			prefix_ = () => a.uid + (prefix == "" ? "" : " " + prefix);
		}

		public bool Enabled
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
			//if (IsEnabled())
			//	Out(VerboseLevel, $"{Prefix}: {s}");
		}

		public void Info(string s)
		{
			if (IsEnabled())
				Out(InfoLevel, $"{Prefix}: {s}");
		}

		public void Warning(string s)
		{
			Out(WarningLevel, $"{Prefix}: {s}");
		}

		public void Error(string s)
		{
			Out(ErrorLevel, $"{Prefix}: {s}");
		}

		public void ErrorST(string s)
		{
			Out(ErrorLevel, $"{Prefix}: {s}\n{new StackTrace(1)}");
		}

		private bool IsEnabled()
		{
			return enabled_;
		}

		private static void Out(int level, string s)
		{
			var t = DateTime.Now.ToString("hh:mm:ss.fff");
			string p = LevelToShortString(level);

			if (level == ErrorLevel)
				AlternateUI.Instance.Sys.LogError($"{t} !![{p}] {s}");
			else
				AlternateUI.Instance.Sys.LogMessage($"{t}   [{p}] {s}");
		}
	}
}
