using LevelGeneration;

namespace LoadingBars
{
    public struct PlayerLoadingState
    {
        public static readonly int NumBatches = EnumUtil.GetValueLength<LG_Factory.BatchName>();
        public int Batch = -1;
        public int BatchCount = 0;
        public int BatchCountRemaining = 0;
        public bool IsDone = false;

        public PlayerLoadingState()
        {
            Batch = -1;
            BatchCount = 0;
            BatchCountRemaining = 0;
            IsDone = false;
        }

        public readonly LG_Factory.BatchName BatchName => Batch >= 0 ? (LG_Factory.BatchName)Batch : LG_Factory.BatchName.Setup;
        public readonly float BatchProgress => BatchCountMax > 0 ? (float) BatchCount / BatchCountMax : 0;
        public readonly int BatchCountMax => BatchCount + BatchCountRemaining;

        public void Reset()
        {
            Batch = -1;
            BatchCount = 0;
            BatchCountRemaining = 0;
            IsDone = false;
        }
    }
}
