using System;
using System.Collections.Generic;
using System.Text;

namespace AUI
{
	public interface IScript
	{
	}

#if !MOCK
	public class VamScript : MVRScript, IScript
	{
		public VamScript()
		{
			new AlternateUI(new VamSys(this));
		}

		public void Update()
		{
			AlternateUI.Instance.Update();
		}

		public void OnEnable()
		{
			AlternateUI.Instance.OnEnable();
		}

		private void OnDisable()
		{
			AlternateUI.Instance.OnDisable();
		}
	}
#else
	public class MockScript : IScript
	{
		public MockScript(string root, string packages)
		{
			new AlternateUI(new FSSys(root, packages));
		}
	}
#endif
}
