using OData.API.Helpers;
using OData.DAL.DbInitializer;
using OData.Model.EntityModels;
using System;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;

namespace OData.API.Controllers
{
    public class PeopleController : ODataController
    {
        private readonly ODataDbContext _dbContext = new ODataDbContext();


        [EnableQuery(MaxExpansionDepth = 3, MaxSkip = 10, MaxTop = 5, PageSize = 4)] // default = 2
        public IHttpActionResult Get()
        {
            return Ok(_dbContext.People);
        }


        [EnableQuery]
        public IHttpActionResult Get([FromODataUri] int key)
        {
            var person = _dbContext.People.Where(p => p.PersonId == key);

            if (!person.Any())
                return NotFound();

            //return Ok(person);
            return Ok(SingleResult.Create(person));
        }


        [HttpGet]
        [ODataRoute("People({key})/Email")]
        [ODataRoute("People({key})/FirstName")]
        [ODataRoute("People({key})/LastName")]
        [ODataRoute("People({key})/DateOfBirth")]
        [ODataRoute("People({key})/Gender")]
        public IHttpActionResult GetPersonProperty([FromODataUri] int key)
        {
            var person = _dbContext.People.FirstOrDefault(p => p.PersonId == key);
            if (person == null)
                return NotFound();

            var propertyToGet = Url.Request.RequestUri.Segments.Last();
            if (!person.HasProperty(propertyToGet))
                return NotFound();

            var propertyValue = person.GetValue(propertyToGet);
            if (propertyValue == null)
                return StatusCode(HttpStatusCode.NoContent);

            return this.CreateOkHttpActionResult(propertyValue);
        }


        [HttpGet]
        [ODataRoute("People({key})/Email/$value")]
        [ODataRoute("People({key})/FirstName/$value")]
        [ODataRoute("People({key})/LastName/$value")]
        [ODataRoute("People({key})/DateOfBirth/$value")]
        [ODataRoute("People({key})/Gender/$value")]
        public IHttpActionResult GetPersonPropertyRawValue([FromODataUri] int key)
        {
            var person = _dbContext.People.FirstOrDefault(p => p.PersonId == key);
            if (person == null)
                return NotFound();

            var propertyToGet = Url.Request.RequestUri
                .Segments[Url.Request.RequestUri.Segments.Length - 2].TrimEnd('/');
            if (!person.HasProperty(propertyToGet))
                return NotFound();

            var propertyValue = person.GetValue(propertyToGet);
            if (propertyValue == null)
                return StatusCode(HttpStatusCode.NoContent);

            return this.CreateOkHttpActionResult(propertyValue.ToString());
        }


        [HttpGet]
        [ODataRoute("People({key})/Friends")]
        public IHttpActionResult GetPersonCollectionProperty([FromODataUri] int key)
        {
            var collectionPropertyToGet = Url.Request.RequestUri.Segments.Last();

            var person = _dbContext.People
                .Include(collectionPropertyToGet)
                .FirstOrDefault(p => p.PersonId == key);

            if (person == null)
                return NotFound();

            var collectionPropertyValue = person.GetValue(collectionPropertyToGet);

            return collectionPropertyValue == null
                ? StatusCode(HttpStatusCode.NoContent)
                : this.CreateOkHttpActionResult(collectionPropertyValue);
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("People({key})/VinylRecords")]
        public IHttpActionResult GetVinylRecordForPerson([FromODataUri] int key)
        {
            var person = _dbContext.People
                .FirstOrDefault(p => p.PersonId == key);

            if (person == null)
                return NotFound();

            return Ok(_dbContext.VinylRecords
                .Include("DynamicVinylRecordProperties")
                .Where(v => v.Person.PersonId == key));
        }


        [HttpGet]
        [EnableQuery]
        [ODataRoute("People({key})/VinylRecords({vinylRecordKey})")]
        public IHttpActionResult GetVinylRecordForPerson([FromODataUri] int key,
            [FromODataUri] int vinylRecordKey)
        {
            var person = _dbContext.People
                .FirstOrDefault(p => p.PersonId == key);

            if (person == null)
                return NotFound();

            var vinylRecord = _dbContext.VinylRecords
                .Include("DynamicVinylRecordProperties")
                .Where(v => v.Person.PersonId == key && v.VinylRecordId == vinylRecordKey);

            if (!vinylRecord.Any())
                return NotFound();

            return Ok(SingleResult.Create(vinylRecord));
        }

        [HttpPost]
        public IHttpActionResult Post(Person person)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _dbContext.People.Add(person);
            _dbContext.SaveChanges();

            return Created(person);
        }

        [HttpPost]
        [ODataRoute("People({key})/VinylRecords")]
        public IHttpActionResult CreateVinylRecordForPerson([FromODataUri] int key,
            VinylRecord vinylRecord)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var person = _dbContext.People.FirstOrDefault(p => p.PersonId == key);
            if (person == null)
                return NotFound();

            vinylRecord.Person = person;

            _dbContext.VinylRecords.Add(vinylRecord);
            _dbContext.SaveChanges();

            return Created(vinylRecord);
        }


        // PUT is for full updates
        [HttpPut]
        [ODataRoute("People({key})")]
        public IHttpActionResult Put([FromODataUri] int key, Person person)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentPerson = _dbContext.People
                .FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
                return NotFound();

            // Alternative: if the person isn't found: Upsert.  This must only
            // be used if the responsibility for creating the key isn't at 
            // server-level.
            //if (currentPerson == null)
            //{
            //    // the key from the URI is the key we should use
            //    person.PersonId = key;
            //    _dbContext.People.Add(person);
            //    _dbContext.SaveChanges();
            //    return Created(person);
            //}

            // if there's an ID property, this should be ignored. 
            person.PersonId = currentPerson.PersonId;
            _dbContext.Entry(currentPerson).CurrentValues.SetValues(person);
            _dbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }


        // PATCH odata/People('key')
        // alternative: attribute routing  
        // PATCH is for partial updates
        [HttpPatch]
        [ODataRoute("People({key})")]
        public IHttpActionResult Patch([FromODataUri] int key, Delta<Person> patch)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentPerson = _dbContext.People
                .FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
                return NotFound();

            // Alternative: if the person isn't found: Upsert.  
            //if (currentPerson == null)
            //{
            //    var person = new Person();
            //    person.PersonId = key;
            //    patch.Patch(person);
            //    _dbContext.People.Add(person);
            //    _dbContext.SaveChanges();
            //    return Created(person);
            //}

            patch.Patch(currentPerson);
            _dbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPatch]
        [ODataRoute("People({key})/VinylRecords({vinylRecordKey})")]
        public IHttpActionResult PartialUpdateVinylRecord([FromODataUri] int key,
            [FromODataUri] int vinylRecordKey, Delta<VinylRecord> patch)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentPerson = _dbContext.People
                .FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
                return NotFound();

            var vinylRecord = _dbContext.VinylRecords
                .Include("DynamicVinylRecordProperties")
                .FirstOrDefault(v => v.Person.PersonId == key && v.VinylRecordId == vinylRecordKey);

            if (vinylRecord == null)
                return NotFound();


            patch.Patch(vinylRecord);
            _dbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE odata/People('key')
        // alternative: attribute routing
        [HttpDelete]
        [ODataRoute("People({key})")]
        public IHttpActionResult Delete([FromODataUri] int key)
        {
            var currentPerson = _dbContext.People.Include("Friends").FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
                return NotFound();

            // this person might be another person's friend, we
            // need to this person from their friend collections
            var peopleWithCurrentPersonAsFriend =
                _dbContext.People.Include("Friends")
                    .Where(p => p.Friends.Select(f => f.PersonId)
                        .AsQueryable()
                        .Contains(key));

            foreach (var person in peopleWithCurrentPersonAsFriend.ToList())
                person.Friends.Remove(currentPerson);

            _dbContext.People.Remove(currentPerson);
            _dbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }



        [HttpDelete]
        [ODataRoute("People({key})/VinylRecords({vinylRecordKey})")]
        public IHttpActionResult DeleteVinylRecordForPerson([FromODataUri] int key
            , [FromODataUri] int vinylRecordKey)
        {
            var currentPerson = _dbContext.People
                .FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
                return NotFound();

            var vinylRecord =
                _dbContext.VinylRecords
                .FirstOrDefault(v => v.Person.PersonId == key && v.VinylRecordId == vinylRecordKey);

            if (vinylRecord == null)
                return NotFound();


            _dbContext.VinylRecords.Remove(vinylRecord);
            _dbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST odata/People('key')/Friends/$ref
        [HttpPost]
        [ODataRoute("People({key})/Friends/$ref")]
        public IHttpActionResult CreateLinkToFriend([FromODataUri] int key, [FromBody] Uri link)
        {
            var currentPerson = _dbContext.People.Include("Friends")
                .FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
                return NotFound();

            // we need the key value from the passed-in link Uri
            var keyOfFriendToAdd = Request.GetKeyValue<int>(link);

            if (currentPerson.Friends.Any(item => item.PersonId == keyOfFriendToAdd))
                return BadRequest($"The person with Id {key} is already linked to the person with Id {keyOfFriendToAdd}");

            var friendToLinkTo = _dbContext.People
                .FirstOrDefault(p => p.PersonId == keyOfFriendToAdd);
            if (friendToLinkTo == null)
                return NotFound();

            currentPerson.Friends.Add(friendToLinkTo);
            _dbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }


        // PUT odata/People('key')/Friends/$ref?$id={'relatedKey'}
        [HttpPut]
        [ODataRoute("People({key})/Friends({relatedKey})/$ref")]
        public IHttpActionResult UpdateLinkToFriend([FromODataUri] int key,
            [FromODataUri] int relatedKey, [FromBody] Uri link)
        {
            var currentPerson = _dbContext.People.Include("Friends").FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
                return NotFound();

            var currentfriend = currentPerson.Friends.FirstOrDefault(item => item.PersonId == relatedKey);
            if (currentfriend == null)
                return NotFound();


            // we need the key value from the passed-in link Uri
            var keyOfFriendToAdd = Request.GetKeyValue<int>(link);
            if (currentPerson.Friends.Any(item => item.PersonId == keyOfFriendToAdd))
                return BadRequest($"The person with Id {key} is already linked to the person with Id {keyOfFriendToAdd}");

            var friendToLinkTo = _dbContext.People.FirstOrDefault(p => p.PersonId == keyOfFriendToAdd);
            if (friendToLinkTo == null)
                return NotFound();

            currentPerson.Friends.Remove(currentfriend);
            currentPerson.Friends.Add(friendToLinkTo);
            _dbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }


        // DELETE odata/People('key')/Friends/$ref?$id={'relatedUriWithRelatedKey'}
        [HttpDelete]
        [ODataRoute("People({key})/Friends({relatedKey})/$ref")]
        public IHttpActionResult DeleteLinkToFriend([FromODataUri] int key, [FromODataUri] int relatedKey)
        {
            var currentPerson = _dbContext.People.Include("Friends")
                .FirstOrDefault(p => p.PersonId == key);
            if (currentPerson == null)
                return NotFound();

            var friend = currentPerson.Friends
                .FirstOrDefault(item => item.PersonId == relatedKey);
            if (friend == null)
                return NotFound();

            currentPerson.Friends.Remove(friend);
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