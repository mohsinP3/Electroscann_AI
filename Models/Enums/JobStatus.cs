namespace ElectroScanAI.Models.Enums
{
    public enum JobStatus
    {
        Open,
        InProgress,
        In_Progress = InProgress,
        Completed,
        Cancelled,
        Closed = Cancelled,
        Filled
    }
}
