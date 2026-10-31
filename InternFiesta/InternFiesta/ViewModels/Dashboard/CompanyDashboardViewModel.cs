
using System;
using System.Collections.Generic;

namespace InternFiesta.ViewModels.Dashboard
{
    public class CompanyDashboardViewModel
    {
        public int TotalPostings { get; set; }
        public int TotalApplications { get; set; }
        public int SelectedStudents { get; set; }
        public int ScheduledInterviews { get; set; }

        public int AppliedCount { get; set; }
        public int ShortlistedCount { get; set; }
        public int SelectedCount { get; set; }
        public int RejectedCount { get; set; }
        public int WaitlistedCount { get; set; }

        public int AwaitingInterviewCount { get; set; }

        public List<CompanyRecentPostingViewModel> RecentPostings
        { get; set; } = new();

        public List<CompanyInterviewCandidateViewModel> PendingInterviews
        { get; set; } = new();
    }

    public class CompanyRecentPostingViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public int ApplicationCount { get; set; }
        public DateTime Deadline { get; set; }
        public bool IsActive { get; set; }
    }

    public class CompanyInterviewCandidateViewModel
    {
        public int ApplicationId { get; set; }
        public string StudentName { get; set; } = "";
        public string InternshipTitle { get; set; } = "";
        public DateTime AppliedAt { get; set; }
    }
}
