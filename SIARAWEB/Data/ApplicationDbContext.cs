using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore; // Agrega esto
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Models;

namespace SIARAWEB.Data
{
    // Agrega la interfaz IDataProtectionKeyContext al final
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Tus tablas actuales
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<DocenteAsignatura> DocenteAsignaturas { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
        public DbSet<AcademicTracking> AcademicTrackings { get; set; }
        public DbSet<Document> Documents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<DocenteAsignatura>()
                .HasKey(da => new { da.DocenteId, da.SubjectId });

            builder.Entity<DocenteAsignatura>()
                .HasOne(da => da.Docente)
                .WithMany(d => d.DocenteAsignaturas)
                .HasForeignKey(da => da.DocenteId);

            builder.Entity<DocenteAsignatura>()
                .HasOne(da => da.Subject)
                .WithMany(s => s.DocenteAsignaturas)
                .HasForeignKey(da => da.SubjectId);
        }
    }
}