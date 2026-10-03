using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace PaletsWebApp.Models
{
    public class ImageTransferModel
    {
        public int TransferenciaId { get; set; }
        public string IdUserProcesa { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public string Observaciones { get; set; } = string.Empty;
       
        [NotMapped]
        public byte[]? ImageArray { get; set; }
    }
}


