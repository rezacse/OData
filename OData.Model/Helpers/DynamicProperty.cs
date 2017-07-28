using Newtonsoft.Json;
using OData.Model.EntityModels;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OData.Model.Helpers
{
    public class DynamicProperty
    {
        [Key]
        [Column(Order = 1)]
        public string Key { get; set; }
        public string SerializedValue { get; set; }

        [Key]
        [Column(Order = 2)]
        public int VinylRecordId { get; set; }

        public virtual VinylRecord VinylRecord { get; set; }

        public object Value
        {
            get { return JsonConvert.DeserializeObject(SerializedValue); }
            set { SerializedValue = JsonConvert.SerializeObject(value); }
        }
    }
}
