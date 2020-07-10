using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SunServices.Helpers;

namespace SunServices.Restapi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdminsTimeSpendController : ControllerBase
    {
        private readonly ILogger<AdminsTimeSpendController> _logger;
        private readonly IConfiguration _config;

        public AdminsTimeSpendController(ILogger<AdminsTimeSpendController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _config = configuration;
        }

        [HttpGet]
        public IEnumerable Get()
        {
            object data = FileDataHelper.Read<object>("AdminsTimeSpend", _config["PathToData"]);
            return JsonConvert.SerializeObject(data);
        }
    }
}
