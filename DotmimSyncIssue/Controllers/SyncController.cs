namespace WebApi.Controllers
{
    using System.Threading.Tasks;
    using Dotmim.Sync.Web.Server;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Sync controller.
    /// </summary>
    /// <seealso cref="ControllerBase" />
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        // The WebServerManager instance is useful to manage all
        // the Web server orchestrators registered in the Startup.cs
        private WebServerManager manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="SyncController"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        public SyncController(WebServerManager manager) => this.manager = manager;

        /// <summary>
        /// This POST handler is mandatory to handle all the sync process.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpPost]
        public async Task Post() => await manager.HandleRequestAsync(HttpContext);

        /// <summary>
        /// This GET handler is optional. It allows you to see the configuration hosted on the server
        /// The configuration is shown only if Environmenent == Development.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [HttpGet]
        public async Task Get() => await manager.HandleRequestAsync(HttpContext);
    }
}
