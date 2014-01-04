using UAParser;

namespace IISReports.Models
{
    public class AgentViewModel
    {
        private static readonly Parser parser = Parser.GetDefault();

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentViewModel"/> class.
        /// </summary>
        /// <param name="userAgent">The user agent.</param>
        /// <param name="count">The count.</param>
        public AgentViewModel(string userAgent, int count)
        {
            this.UserAgent = userAgent;
            this.Count = count;
            var info = parser.Parse(userAgent);
            this.AgentInfo = info.UserAgent;
            this.Device = info.Device.ToString();
            this.OS = info.OS.ToString();
        }

        public string UserAgent { get; private set; }
        public int Count { get; private set; }

        public string OS { get; private set; }
        public string Device { get; private set; }
        public UserAgent AgentInfo { get; private set; }
    }
}