using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestObjectService.Models
{
    public class SniffingPoint
    {
        [Key]
        public Guid? Id { get; set; }
        public string Name { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public Guid? TestObjectId { get; set; }
    }
}
