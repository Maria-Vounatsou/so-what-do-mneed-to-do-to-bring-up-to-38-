namespace WrikeTimeLogger.Models;


public class TaskResponse
{
    public string kind { get; set; }
    public WrikeTask[] data { get; set; }
}

public class WrikeTask
{
    public string id { get; set; }
    public string accountId { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public string briefDescription { get; set; }
    public string[] parentIds { get; set; }
    public object[] superParentIds { get; set; }
    public string[] sharedIds { get; set; }
    public string[] responsibleIds { get; set; }
    public string status { get; set; }
    public string importance { get; set; }
    public DateTime createdDate { get; set; }
    public DateTime updatedDate { get; set; }
    public Dates dates { get; set; }
    public string scope { get; set; }
    public string[] authorIds { get; set; }
    public string customStatusId { get; set; }
    public bool hasAttachments { get; set; }
    public string permalink { get; set; }
    public string priority { get; set; }
    public bool followedByMe { get; set; }
    public string[] followerIds { get; set; }
    public object[] superTaskIds { get; set; }
    public object[] subTaskIds { get; set; }
    public object[] dependencyIds { get; set; }
    public object[] metadata { get; set; }
    public object[] customFields { get; set; }
}

public class Dates
{
    public string type { get; set; }
}



