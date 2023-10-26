using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestObjectService.Models
{
    public class TestObject
    {
        [Key]
        public int Id { get; set; }
        public string Type { get; set; }
        public int SerialNr { get; set; }
        public int MachineNr { get; set; }
        public string ImageUrl { get; set; }
    }
}
