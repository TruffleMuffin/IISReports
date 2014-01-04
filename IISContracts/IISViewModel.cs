using System;

namespace IISContracts
{
    public class IISViewModel
    {
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
        public string UserName { get; set; }
        public string Verb { get; set; }
        public string Url { get; set; }
        public string QueryString { get; set; }
        public string Port { get; set; }
        public string UserAgent { get; set; }
        public int ResponseStatusCode { get; set; }
        public int ResponseStatusSubCode { get; set; }
        public string TimeTaken { get; set; }

        public string Id
        {
            get { return Timestamp.ToString("HH:mm:ss dd/MM/yyyy") + "-" + UserName + "-" + UserAgent + "-" + Url + "-" + QueryString; }
        }
    }
}