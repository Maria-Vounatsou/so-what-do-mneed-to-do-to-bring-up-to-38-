namespace WrikeTimeLogger.Models
{
    public class Workflows
    {
        public string kind { get; set; }
        public Data[] data { get; set; }
    }

    public class Data
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool standard { get; set; }
        public bool hidden { get; set; }
        public Customstatus[] customStatuses { get; set; }
    }

    public class Customstatus
    {
        public string id { get; set; }
        public string name { get; set; }
        public bool standardName { get; set; }
        public string color { get; set; }
        public bool standard { get; set; }
        public string group { get; set; }
        public bool hidden { get; set; }
    }

    public class MyWorkFlows
    {
        public string workflowId { get; set; }
        public string workflowName { get; set; }
        public string customStatusName { get; set; }
    }
}


