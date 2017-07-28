using OData.API.Helpers;
using OData.DAL.DbInitializer;
using OData.Model.EntityModels;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;

namespace OData.API.Controllers
{
    public class SingletonController : ODataController
    {
        private readonly ODataDbContext _dbContext = new ODataDbContext();
        private const int TimId = 6;


        [HttpGet]
        [ODataRoute("Tim")]
        public IHttpActionResult GetSingletonTim()
        {
            var personTim = _dbContext.People.FirstOrDefault(p => p.PersonId == TimId);

            return Ok(personTim);
        }

        [HttpGet]
        [ODataRoute("Tim/Email")]
        [ODataRoute("Tim/FirstName")]
        [ODataRoute("Tim/LastName")]
        [ODataRoute("Tim/DateOfBirth")]
        [ODataRoute("Tim/Gender")]
        public IHttpActionResult GetPersonProperty()
        {
            var person = _dbContext.People.FirstOrDefault(p => p.PersonId == TimId);
            if (person == null)
                return NotFound();

            var propertyToGet = Url.Request.RequestUri.Segments.Last();
            if (!person.HasProperty(propertyToGet))
                return NotFound();

            var propertyValue = person.GetValue(propertyToGet);

            return propertyValue == null
                ? StatusCode(HttpStatusCode.NoContent)
                : this.CreateOkHttpActionResult(propertyValue);
        }

        [HttpGet]
        [ODataRoute("Tim/Email/$value")]
        [ODataRoute("Tim/FirstName/$value")]
        [ODataRoute("Tim/LastName/$value")]
        [ODataRoute("Tim/DateOfBirth/$value")]
        [ODataRoute("Tim/Gender/$value")]
        public IHttpActionResult GetPersonPropertyRawValue()
        {
            var person = _dbContext.People.FirstOrDefault(p => p.PersonId == TimId);
            if (person == null)
                return NotFound();

            var propertyToGet = Url.Request.RequestUri
                .Segments[Url.Request.RequestUri.Segments.Length - 2].TrimEnd('/');

            if (!person.HasProperty(propertyToGet))
                return NotFound();

            var propertyValue = person.GetValue(propertyToGet);

            return propertyValue == null
                ? StatusCode(HttpStatusCode.NoContent)
                : this.CreateOkHttpActionResult(propertyValue.ToString());
        }

        [HttpGet]
        [ODataRoute("Tim/Friends")]
        public IHttpActionResult GetPersonCollectionProperty()
        {
            var collectionPropertyToGet = Url.Request.RequestUri.Segments.Last();

            var person = _dbContext.People
                .Include(collectionPropertyToGet)
                .FirstOrDefault(p => p.PersonId == TimId);

            if (person == null)
                return NotFound();

            var collectionPropertyValue = person.GetValue(collectionPropertyToGet);

            return collectionPropertyValue == null
                ? StatusCode(HttpStatusCode.NoContent)
                : this.CreateOkHttpActionResult(collectionPropertyValue);
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("Tim/VinylRecords")]
        public IHttpActionResult GetVinylRecordForPerson()
        {
            var person = _dbContext.People
                .FirstOrDefault(p => p.PersonId == TimId);

            if (person == null)
                return NotFound();

            return Ok(_dbContext.VinylRecords.Where(v => v.Person.PersonId == TimId));
        }

        [HttpPatch]
        [ODataRoute("Tim")]
        public IHttpActionResult Patch(Delta<Person> patch)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentPerson = _dbContext.People
                .FirstOrDefault(p => p.PersonId == TimId);

            patch.Patch(currentPerson);
            _dbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }


        protected override void Dispose(bool disposing)
        {
            _dbContext.Dispose();
            base.Dispose(disposing);
        }
    }
}