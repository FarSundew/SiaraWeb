using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SIARAWEB.Models;

namespace SIARAWEB.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // --- TABLAS PARA PERIODOS Y CALENDARIO DE CORTES ---
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        public DbSet<AcademicPeriod> AcademicPeriods { get; set; }
        public DbSet<CutoffDate> CutoffDates { get; set; }

        // --- TABLAS ACTUALES ---
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<DocenteAsignatura> DocenteAsignaturas { get; set; }
        public DbSet<AcademicTracking> AcademicTrackings { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Departamento> Departamentos { get; set; }
        public DbSet<TrackingDeadline> TrackingDeadline { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Configuración de la relación M:N de DocenteAsignatura
            builder.Entity<DocenteAsignatura>()
                .HasKey(da => new { da.DocenteId, da.SubjectId });

            builder.Entity<DocenteAsignatura>()
                .HasOne(da => da.Docente)
                .WithMany(d => d.DocenteAsignaturas)
                .HasForeignKey(da => da.DocenteId)
                .OnDelete(DeleteBehavior.Restrict); // Evita borrado en cascada

            builder.Entity<DocenteAsignatura>()
                .HasOne(da => da.Subject)
                .WithMany(s => s.DocenteAsignaturas)
                .HasForeignKey(da => da.SubjectId)
                .OnDelete(DeleteBehavior.Restrict); // Evita borrado en cascada

            // 2. Apagar explícitamente el Cascade Delete en las relaciones de Subject
            // Esto evita por completo el error de "multiple cascade paths" en SQL Server
            builder.Entity<Subject>()
                .HasOne(s => s.AcademicPeriod)
                .WithMany(p => p.Subjects)
                .HasForeignKey(s => s.AcademicPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Subject>()
                .HasOne(s => s.Departamento)
                .WithMany(d => d.Subjects)
                .HasForeignKey(s => s.DepartamentoId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Romper rutas de cascada indirectas que pueden llegar a Subject
            // Evitar cascadas desde AcademicPeriod -> CutoffDate -> AcademicTracking -> Subject
            builder.Entity<CutoffDate>()
                .HasOne(cd => cd.AcademicPeriod)
                .WithMany() // evita depender de la propiedad de navegación si no existe
                .HasForeignKey(cd => cd.AcademicPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AcademicTracking>()
                .HasOne(at => at.CutoffDate)
                .WithMany() // si CutoffDate no expone colección, usar esta sobrecarga
                .HasForeignKey(at => at.CutoffDateId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AcademicTracking>()
                .HasOne(at => at.Subject)
                .WithMany(s => s.AcademicTrackings)
                .HasForeignKey(at => at.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Si Document también referencia Subject/CutoffDate, evitar cascada ahí
            builder.Entity<Document>()
                .HasOne(d => d.Subject)
                .WithMany(s => s.Documents)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Document>()
                .HasOne(d => d.CutoffDate)
                .WithMany()
                .HasForeignKey(d => d.CutoffDateId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}