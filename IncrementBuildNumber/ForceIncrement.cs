namespace IncrementBuildNumber
{
    internal enum ForceIncrement
    {
        None,
        MinorAndReset,
        MajorAndReset,
        MinorAndBuild,
        MajorAndBuild,
        Build
    }
}