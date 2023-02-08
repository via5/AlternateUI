namespace AUI
{
	abstract class MRUAtomUIModifier : AtomUIModifier
	{
		public MRUAtomUIModifier(string logPrefix)
			: base(logPrefix)
		{
		}

		public void UpdateOthers(MRUAtomInfo like)
		{
			for (int i = 0; i < Atoms.Count; ++i)
			{
				var a = Atoms[i] as MRUAtomInfo;
				if (a == like)
					continue;

				if (a.IsLike(like))
					a.MakeStale();
			}
		}
	}
}
