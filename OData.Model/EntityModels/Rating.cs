using System.ComponentModel.DataAnnotations;

namespace OData.Model.EntityModels
{
    public class Rating
    {
        [Key]
        public int RatingId { get; set; }

        [Required]
        public int Value { get; set; }

        [Required]
        public Person RatedBy { get; set; }
    }
}
