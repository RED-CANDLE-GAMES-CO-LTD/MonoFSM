using System;

namespace Auto.Utils{
	[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
	public class ScriptTiming : Attribute
	{
		public readonly int timing_offset;

		public ScriptTiming(int timing_offset)
		{
			this.timing_offset = timing_offset;
		}
	}
}