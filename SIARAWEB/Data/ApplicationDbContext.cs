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

        // --- LLAVES DE PROTECCIÓN DE DATOS (Identity / Cookies / Antiforgery) ---
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        // --- PERIODOS ESCOLARES Y BUZONES/FECHAS DE CORTE ---
        public DbSet<AcademicPeriod> AcademicPeriods { get; set; }
        public DbSet<CutoffDate> CutoffDates { get; set; }
        public DbSet<DocumentTask> DocumentTasks { get; set; }
        public DbSet<TrackingDeadline> TrackingDeadline { get; set; } = default!;

        // --- ASIGNATURAS, USUARIOS Y DEPARTAMENTOS ---
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<DocenteAsignatura> DocenteAsignaturas { get; set; }
        public DbSet<Departamento> Departamentos { get; set; }

        // --- ENTREGAS Y EVALUACIONES ---
        public DbSet<Document> Documents { get; set; }
        public DbSet<AcademicTracking> AcademicTrackings { get; set; }
        public DbSet<FinalSubjectGrade> FinalSubjectGrades { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // =========================================================================
            // 1. TABLA INTERMEDIA: DocenteAsignatura (Clave Compuesta Original)
            // =========================================================================
            builder.Entity<DocenteAsignatura>()
                .HasKey(da => new { da.DocenteId, da.SubjectId });

            builder.Entity<DocenteAsignatura>()
                .HasOne(da => da.Docente)
                .WithMany(d => d.DocenteAsignaturas)
                .HasForeignKey(da => da.DocenteId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DocenteAsignatura>()
                .HasOne(da => da.Subject)
                .WithMany(s => s.DocenteAsignaturas)
                .HasForeignKey(da => da.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================================================================
            // 2. DEPARTAMENTO Y USUARIO (Sin conflicto bidireccional)
            // =========================================================================
            builder.Entity<Departamento>()
                .HasOne(d => d.HeadOfDepartment)
                .WithMany()
                .HasForeignKey(d => d.HeadOfDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Departamento)
                .WithMany()
                .HasForeignKey(u => u.DepartamentoId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================================================================
            // 3. RESTRICCIÓN DE CASCADA EN SUBJECT
            // =========================================================================
            builder.Entity<DocenteAsignatura>()
                .HasOne(da => da.AcademicPeriod)
                .WithMany()
                .HasForeignKey(da => da.AcademicPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Subject>()
                .HasOne(s => s.Departamento)
                .WithMany(d => d.Subjects)
                .HasForeignKey(s => s.DepartamentoId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================================================================
            // 4. ENTIDAD TAREA / BUZÓN (DocumentTask)
            // =========================================================================
            builder.Entity<DocumentTask>()
                .HasOne(t => t.AcademicPeriod)
                .WithMany()
                .HasForeignKey(t => t.AcademicPeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DocumentTask>()
                .HasOne(t => t.Departamento)
                .WithMany()
                .HasForeignKey(t => t.DepartamentoId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================================================================
            // 5. CORTES Y ENTREGAS DOCUMENTALES (Document)
            // =========================================================================
            builder.Entity<Document>()
                .HasOne(d => d.Subject)
                .WithMany(s => s.Documents)
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Document>()
                .HasOne(d => d.DocumentTask)
                .WithMany(t => t.Documents)
                .HasForeignKey(d => d.DocumentTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Document>()
                .HasOne(d => d.CutoffDate)
                .WithMany()
                .HasForeignKey(d => d.CutoffDateId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================================================================
            // 6. SEGUIMIENTO ACADÉMICO / CALIFICACIONES (AcademicTracking)
            // =========================================================================
            builder.Entity<AcademicTracking>()
                .HasOne(at => at.Subject)
                .WithMany(s => s.AcademicTrackings)
                .HasForeignKey(at => at.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AcademicTracking>()
                .HasOne(at => at.DocumentTask)
                .WithMany(t => t.AcademicTrackings)
                .HasForeignKey(at => at.DocumentTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<AcademicTracking>()
                .HasOne(at => at.CutoffDate)
                .WithMany()
                .HasForeignKey(at => at.CutoffDateId)
                .OnDelete(DeleteBehavior.Restrict);

            // =========================================================================
            // 7. COMPATIBILIDAD CON CORTES PREVIOS (CutoffDate)
            // =========================================================================
            builder.Entity<CutoffDate>()
                .HasOne(cd => cd.AcademicPeriod)
                .WithMany()
                .HasForeignKey(cd => cd.AcademicPeriodId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
