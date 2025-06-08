namespace Fusion.Addons.FSM
{
	public static class NumberExtensions
	{
		public static bool IsBitSet(this int flags, int bit)
		{
			return (flags & (1 << bit)) == (1 << bit);
		}

		public static int SetBit(ref this int flags, int bit, bool value)
		{
			return value == true ? flags |= (1 << bit) : flags &= ~(1 << bit);
		}
		
		public static int SetBitNoRef(this int flags, int bit, bool value)
		{
			return value == true ? flags |= 1 << bit : flags &= ~(1 << bit);
		}
	}
}
