using System.ComponentModel.DataAnnotations;

namespace ForumDyskusyjne.Models;

public enum SeverityLevel
{
    Warning,
    Block,
    AutoDelete
}

public enum MatchType
{
    Exact,
    Contains,
    Regex
}

public enum ReportStatus
{
    Pending,
    InReview,
    Resolved, 
    Dismissed
}

public enum ChangeReason
{
    Automatic,
    ManualPromotion,
    ManualDemotion
}

public enum ActionTaken
{
    Warning,
    Blocked,
    UserBanned
}
