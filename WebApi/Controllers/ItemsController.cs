using DataEntities;
using DataEntities.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly IConfiguration configuration;

        public ItemsController(ILogger<ItemsController> logger, IConfiguration configuration)
        {
            _ = logger;
            this.configuration = configuration;
        }

        [HttpGet]
        public IEnumerable<Item> Get()
        {
            var connectionString = this.configuration.GetConnectionString("SqlServer");
            using (var dbContext = new MyDbContextSqlServer(new DbContextOptionsBuilder().UseSqlServer($"{connectionString}").EnableDetailedErrors().Options))
            {
                return dbContext.Items.ToArray();
            }
        }
    }
}
