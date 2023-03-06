using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace OKAPI.Models
{
    public enum EXISTENCETYPES
    {
        FactoryOrder = 1,
        IndividualNew = 2,
        IndividualUsed = 3
    }

    [Table("ModelPriceForOnline", Schema = "Price")]
    public class ModelsData
    {
        [Key]
        public string? ComissionNumber { get; set; } //modelcode for existencetype=1 (factory order) / comissionumber for existencetype=2 (individual)
        public int? ExistenceType { get; set; }
        public string? ModelCodeLong { get; set; }   
        public string? Make { get; set; }
        public string? ColorCode { get; set; }
        public string? ColorCodeInterior { get; set; }
        public int? ActiveInSpider { get; set; }
        public virtual ICollection<AdditionalAccessory>? AdditionalAccessories { get; set; }

    }

    [Table("ModelPriceForOnlineAccessories", Schema = "Price")]
    public class AdditionalAccessory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]        
        public Guid guid { get; set; }        
        [ForeignKey("ModelNP")]
        public string? ComissionNumber { get; set; }
        public string? PrNumber { get; set; }       
        public string? Description { get; set; }    
        public virtual ModelsData? ModelNP { get; set; }
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
    
    
    
    public class ModelDataDbContext : DbContext
    {
        public ModelDataDbContext(DbContextOptions<ModelDataDbContext> options)
        : base(options)
        { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AdditionalAccessory>()
            .HasOne(p => p.ModelNP)
            .WithMany(b => b.AdditionalAccessories)
            .HasForeignKey(p => p.ComissionNumber)
            .HasPrincipalKey(b => b.ComissionNumber);
        }
        public DbSet<ModelsData> ModelsTable { get; set; }
        public DbSet<AdditionalAccessory> AdditionalAccessories { get; set; }
        public DbSet<ModelImage> ModelsImageTable { get; set; }
 
    }
}
