using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API.DTOs
{
    public class CreateMessageDto
    {
        public string RecepientUsername { get; set; }
        public string Content { get; set; }
    }
}
