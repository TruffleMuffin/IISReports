using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using IISContracts;
using IISReports.Models;
using IISReports.Services;

namespace IISReports.Controllers
{
    [RoutePrefix("API/Logs")]
    public class LogController : ApiController
    {
        private readonly LogService service;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogController"/> class.
        /// </summary>
        public LogController()
        {
            service = LogService.Instance;
        }

        [HttpPost]
        [Route("")]
        public HttpStatusCode Post([FromBody]IEnumerable<IISViewModel> models)
        {
            service.Import(models);
            return HttpStatusCode.OK;
        }

        [HttpGet]
        [Route("Hits/{year:int}/{month:int}")]
        public IEnumerable<HitAndResponseViewModel> GetHits(int year, int month)
        {
            return service.GetHits(year, month);
        }

        [HttpGet]
        [Route("Agents/{year:int}/{month:int}")]
        public IEnumerable<AgentViewModel> GetAgents(int year, int month)
        {
            return service.GetAgents(year, month);
        }
    }
}
