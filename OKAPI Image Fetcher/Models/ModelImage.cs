using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace OKAPI.Models
{
    [Table("ModelPriceForOnline", Schema = "Price")]
    public class ModelsData
    {
        [Key]
        public string? modelCode { get; set; }
        public string? modelCodeLong { get; set; }   
        public string? make { get; set; }

    }

    public class OKAPIImage
    {
        public string? name { get; set; }
        public string? finalName { get;set; }
        public string? url { get; set; }
        public string? finalUrl { get; set; }
    }
    public class OKAPIImageMeta
    {
        public int count { get; set; }
    }
    public class OKAPIImageResponse
    {
        public IEnumerable<OKAPIImage>? data { get; set; } 
        public OKAPIImageMeta? meta { get; set; }
    }

    [Table("ModelImages", Schema = "OKAPI")]
    public class ModelImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid guid { get; set; }
        public string? modelCode { get; set; }
        public string? imageName { get; set; }
        public string? imageUrl { get; set; }
    }
    
    /*
    [Table("Accessories", Schema = "TiNet")]
    public class TiNetData_Accessory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid guid { get; set; }
        [ForeignKey("ModelNP")]
        public string modelCode { get; set; }
        public string name { get; set; }
        public virtual TiNetData_Model ModelNP { get; set; }
    }
    */
    
    public class ModelDataDbContext : DbContext
    {
        public ModelDataDbContext(DbContextOptions<ModelDataDbContext> options)
        : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
        public DbSet<ModelsData> ModelsTable { get; set; }
        public DbSet<ModelImage> ModelsImageTable { get; set; }
 
    }
}
