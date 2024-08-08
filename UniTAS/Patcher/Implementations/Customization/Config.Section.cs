namespace UniTAS.Patcher.Implementations.Customization;

public partial class Config
{
    public static class Sections
    {
        public static class Debug
        {
            public static class FunctionCallTrace
            {
                public const string SECTION_NAME = $"{nameof(Debug)}.FunctionCallTrace";
                public const string ENABLE = "Enable";
                public const string MATCHING_TYPES = "MatchingTypes";
            }
        }
    }
}