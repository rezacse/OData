using OData.DAL.DbInitializer;
using System.Linq;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;

namespace OData.API.Controllers
{
    public class VinylRecordsController : ODataController
    {
        private readonly ODataDbContext _dbContext = new ODataDbContext();

        [HttpGet]
        [ODataRoute("VinylRecords")]
        public IHttpActionResult GetAllVinylRecords()
        {
            return Ok(_dbContext.VinylRecords);
        }


        [HttpGet]
        [ODataRoute("VinylRecords({key})")]
        public IHttpActionResult Get([FromODataUri] int key)
        {
            var vinylRecord = _dbContext.VinylRecords
                .FirstOrDefault(v => v.VinylRecordId == key);

            if (vinylRecord == null)
                return NotFound();

            return Ok(vinylRecord);
        }


        protected override void Dispose(bool disposing)
        {
            _dbContext.Dispose();
            base.Dispose(disposing);
        }
    }
}