namespace AVNAnalytics
{
    /// <summary>Firebase-style event parameter: new AvnParameter("level", 3).</summary>
    public readonly struct AVNGameAnalyticsParameter
    {
        public readonly string Name;
        public readonly object Value;

        public AVNGameAnalyticsParameter(string name, string value) { Name = name; Value = value; }
        public AVNGameAnalyticsParameter(string name, long value) { Name = name; Value = value; }
        public AVNGameAnalyticsParameter(string name, int value) { Name = name; Value = (long)value; }
        public AVNGameAnalyticsParameter(string name, double value) { Name = name; Value = value; }
        public AVNGameAnalyticsParameter(string name, float value) { Name = name; Value = (double)value; }
        public AVNGameAnalyticsParameter(string name, bool value) { Name = name; Value = value ? 1L : 0L; }
    }
}
