using OData.API.Helpers;
using OData.DAL.DbInitializer;
using OData.Model.EntityModels;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Routing;

namespace OData.API.Controllers
{
    public class RecordStoresController : ODataController
    {

        private readonly ODataDbContext _dbContext = new ODataDbContext();


        [EnableQuery]
        public IHttpActionResult Get()
        {
            return Ok(_dbContext.RecordStores);
        }


        [EnableQuery]
        [ODataRoute("RecordStores({key})")]
        public IHttpActionResult Get([FromODataUri] int key)
        {
            var recordStores = _dbContext.RecordStores.Where(p => p.RecordStoreId == key);

            if (!recordStores.Any())
                return NotFound();

            return Ok(SingleResult.Create(recordStores));
        }


        [HttpGet]
        [ODataRoute("RecordStores({key})/Tags")]
        [EnableQuery]
        public IHttpActionResult GetRecordStoreTagsProperty([FromODataUri] int key)
        {
            // no Include necessary for EF - Tags isn't a navigation property 
            // in the entity model.  
            var recordStore = _dbContext.RecordStores
                .FirstOrDefault(p => p.RecordStoreId == key);

            if (recordStore == null)
                return NotFound();

            var collectionPropertyToGet = Url.Request.RequestUri.Segments.Last();
            var collectionPropertyValue = recordStore.GetValue(collectionPropertyToGet);

            // return the collection of tags
            return this.CreateOkHttpActionResult(collectionPropertyValue);
        }

        [HttpPost]
        [ODataRoute("RecordStores")]
        public IHttpActionResult CreateRecordStore(RecordStore recordStore)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _dbContext.RecordStores.Add(recordStore);
            _dbContext.SaveChanges();

            return Created(recordStore);
        }

        [HttpPatch]
        [ODataRoute("RecordStores({key})")]
        [ODataRoute("RecordStores({key})/OData.Model.EntityModels.SpecializedRecordStore")]
        public IHttpActionResult UpdateRecordStorePartially([FromODataUri] int key, Delta<RecordStore> patch)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var currentRecordStore = _dbContext.RecordStores.FirstOrDefault(p => p.RecordStoreId == key);

            if (currentRecordStore == null)
                return NotFound();

            patch.Patch(currentRecordStore);
            _dbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpDelete]
        [ODataRoute("RecordStores({key})")]
        [ODataRoute("RecordStores({key})/OData.Model.EntityModels.SpecializedRecordStore")]
        public IHttpActionResult DeleteRecordStore([FromODataUri] int key)
        {
            var currentRecordStore = _dbContext.RecordStores
                .Include("Ratings")
                .FirstOrDefault(p => p.RecordStoreId == key);

            if (currentRecordStore == null)
                return NotFound();

            currentRecordStore.Ratings.Clear();

            _dbContext.RecordStores.Remove(currentRecordStore);
            _dbContext.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        #region functions

        [HttpGet]
        [ODataRoute("RecordStores({key})/OData.Functions.IsHighRated(minimumRating={minimumRating})")]
        public bool IsHighRated([FromODataUri] int key, int minimumRating)
        {
            var recordStore = _dbContext.RecordStores
                .FirstOrDefault(p => p.RecordStoreId == key
                    && p.Ratings.Any()
                    && p.Ratings.Sum(r => r.Value) / p.Ratings.Count >= minimumRating);

            return recordStore != null;
        }

        [HttpGet]
        [ODataRoute("RecordStores/OData.Functions.AreRatedBy(personIds={personIds})")]
        public IHttpActionResult AreRatedBy([FromODataUri] IEnumerable<int> personIds)
        {
            var recordStores = _dbContext.RecordStores
                .Where(p => p.Ratings.Any(r => personIds.Contains(r.RatedBy.PersonId)));

            return this.CreateOkHttpActionResult(recordStores);
        }

        [HttpGet]
        [ODataRoute("GetHighRatedRecordStores(minimumRating={minimumRating})")]
        public IHttpActionResult GetHighRatedRecordStores([FromODataUri] int minimumRating)
        {
            var recordStores = _dbContext.RecordStores
                .Where(p => p.Ratings.Any()
                    && p.Ratings.Sum(r => r.Value) / p.Ratings.Count >= minimumRating);

            return this.CreateOkHttpActionResult(recordStores);
        }

        #endregion


        #region actions

        [HttpPost]
        [ODataRoute("RecordStores({key})/OData.Actions.Rate")]
        public IHttpActionResult Rate([FromODataUri] int key, ODataActionParameters parameters)
        {
            var recordStore = _dbContext.RecordStores
                .FirstOrDefault(p => p.RecordStoreId == key);

            if (recordStore == null)
                return NotFound();

            int rating;
            int personId;
            object outputFromDictionary;

            if (!parameters.TryGetValue("rating", out outputFromDictionary)
                || !int.TryParse(outputFromDictionary.ToString(), out rating))
                return NotFound();


            if (!parameters.TryGetValue("personId", out outputFromDictionary)
                || !int.TryParse(outputFromDictionary.ToString(), out personId))
                return NotFound();

            var person = _dbContext.People
                .FirstOrDefault(p => p.PersonId == personId);

            if (person == null)
                return NotFound();

            recordStore.Ratings.Add(new Rating
            {
                RatedBy = person,
                Value = rating
            });

            return this.CreateOkHttpActionResult(_dbContext.SaveChanges() > -1);
        }

        [HttpPost]
        [ODataRoute("RecordStores/OData.Actions.RemoveRatings")]
        public IHttpActionResult RemoveRatings(ODataActionParameters parameters)
        {
            int personId;
            object outputFromDictionary;

            if (!parameters.TryGetValue("personId", out outputFromDictionary)
                || !int.TryParse(outputFromDictionary.ToString(), out personId))
                return NotFound();

            var recordStoresRatedByCurrentPerson = _dbContext.RecordStores
                .Include("Ratings")
                .Include("Ratings.RatedBy")
                .Where(p => p.Ratings.Any(r => r.RatedBy.PersonId == personId))
                .ToList();

            foreach (var store in recordStoresRatedByCurrentPerson)
            {
                var ratingsByCurrentPerson = store.Ratings
                    .Where(r => r.RatedBy.PersonId == personId).ToList();

                foreach (var rating in ratingsByCurrentPerson)
                    store.Ratings.Remove(rating);
            }

            return this.CreateOkHttpActionResult(_dbContext.SaveChanges() > -1);
        }

        [HttpPost]
        [ODataRoute("RemoveRecordStoreRatings")]
        public IHttpActionResult RemoveRecordStoreRatings(ODataActionParameters parameters)
        {
            int personId;
            object outputFromDictionary;

            if (!parameters.TryGetValue("personId", out outputFromDictionary)
                || !int.TryParse(outputFromDictionary.ToString(), out personId))
                return NotFound();

            var recordStoresRatedByCurrentPerson = _dbContext.RecordStores
                .Include("Ratings")
                .Include("Ratings.RatedBy")
                .Where(p => p.Ratings.Any(r => r.RatedBy.PersonId == personId))
                .ToList();

            foreach (var store in recordStoresRatedByCurrentPerson)
            {
                var ratingsByCurrentPerson = store.Ratings
                    .Where(r => r.RatedBy.PersonId == personId)
                    .ToList();

                foreach (var rating in ratingsByCurrentPerson)
                    store.Ratings.Remove(rating);
            }

            return StatusCode(_dbContext.SaveChanges() > -1
                ? HttpStatusCode.NoContent
                : HttpStatusCode.InternalServerError);
        }

        #endregion


        #region inheritance

        [HttpGet]
        [EnableQuery]
        [ODataRoute("RecordStores/OData.Model.EntityModels.SpecializedRecordStore")]
        public IHttpActionResult GetSpecializedRecordStores()
        {
            //var specializedStores = _ctx.RecordStores.Where(r => r is SpecializedRecordStore);
            //return Ok(specializedStores);

            // projection, required for filtering
            var specializedStores = _dbContext.RecordStores.Where(r => r is SpecializedRecordStore);
            return Ok(specializedStores.Select(s => s as SpecializedRecordStore));
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("RecordStores({key})/OData.Model.EntityModels.SpecializedRecordStore")]
        public IHttpActionResult GetSpecializedRecordStore([FromODataUri] int key)
        {
            var specializedStores = _dbContext.RecordStores
                .Where(r => r.RecordStoreId == key && r is SpecializedRecordStore);

            if (!specializedStores.Any())
                return NotFound();

            // return the result
            // return Ok(specializedStores.Single());

            // If you want to enable queries on this, you should return
            // an IQueryable result.  This should be used in combination with the
            // EnableQuery attribute - if not, this will fail.
            return Ok(SingleResult.Create(
                specializedStores.Select(s => s as SpecializedRecordStore)));
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            // dispose the context
            _dbContext.Dispose();
            base.Dispose(disposing);
        }
    }
}