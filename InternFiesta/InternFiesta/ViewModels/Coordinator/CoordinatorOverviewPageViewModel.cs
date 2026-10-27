
using System;
using System.Collections.Generic;

namespace InternFiesta.ViewModels.Coordinator
{
    public class CoordinatorOverviewPageViewModel
    {
        public CoordinatorOverviewCountsViewModel Counts
        { get; set; } = new();

        public int TotalAcademicRecords { get; set; }

        public List<CoordinatorAcademicRowViewModel> AcademicRows
        { get; set; } = new();
    }

    public class CoordinatorAcademicRowViewModel
    {
        public int AcademicInternshipId { get; set; }

        public string StudentName { get; set; } = string.Empty;

        public string InternshipTitle { get; set; } = string.Empty;

        public string FacultyName { get; set; } = string.Empty;

        public string ApplicationStatus { get; set; } = string.Empty;

        public DateTime StartedAtUtc { get; set; }

        public int ProgressUpdateCount { get; set; }

        public int MilestoneCount { get; set; }

        public int CompletedMilestoneCount { get; set; }

        public bool HasEvaluation { get; set; }

        // UI tracking indicator, not official academic completion.
        public string TrackingStatus => HasEvaluation
            ? "Evaluated"
            : ProgressUpdateCount > 0
                ? "Updates Received"
                : "Awaiting Updates";
    }
}
