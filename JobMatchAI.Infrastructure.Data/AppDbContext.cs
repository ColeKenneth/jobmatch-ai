using System;
using System.Collections.Generic;
using System.Text;
using JobMatchAI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobMatchAI.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users => Set<User>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Alumni> Alumni => Set<Alumni>();
        public DbSet<Employer> Employers => Set<Employer>();
        public DbSet<JobPosting> JobPostings => Set<JobPosting>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<StudentSkill> StudentSkills => Set<StudentSkill>();
        public DbSet<JobSkill> JobSkills => Set<JobSkill>();
        public DbSet<Certification> Certifications => Set<Certification>();
        public DbSet<Internship> Internships => Set<Internship>();
        public DbSet<EmploymentHistory> EmploymentHistories => Set<EmploymentHistory>();
        public DbSet<MatchResult> MatchResults => Set<MatchResult>();
        public DbSet<PlacementRecord> PlacementRecords => Set<PlacementRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Email).IsUnique();
            });

            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.StudentIdNumber).IsUnique();

                entity.HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade); 
            });

            modelBuilder.Entity<Alumni>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(a => a.User)
                .WithOne(u => u.Alumni)
                .HasForeignKey<Alumni>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Employer>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                .WithOne(u => u.Employer)
                .HasForeignKey<Employer>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<JobPosting>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(jp => jp.Employer)
                .WithMany(e => e.JobPostings)
                .HasForeignKey(jp => jp.EmployerId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Skill>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<StudentSkill>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.StudentId, e.SkillId }).IsUnique();

                entity.HasOne(ss => ss.Student)
                .WithMany(s => s.StudentSkills)
                .HasForeignKey(ss => ss.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ss => ss.Skill)
                .WithMany(s => s.StudentSkills)
                .HasForeignKey(ss => ss.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<JobSkill>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.JobPostingId, e.SkillId }).IsUnique();

                entity.HasOne(js => js.JobPosting)
                .WithMany(jp => jp.RequiredSkills)
                .HasForeignKey(js => js.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(js => js.Skill)
                .WithMany(sk => sk.JobSkills)
                .HasForeignKey(js => js.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Certification>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Internship>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(i => i.Student)
                .WithMany()
                .HasForeignKey(i => i.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<EmploymentHistory>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(eh => eh.Alumni)
                .WithMany(a => a.EmploymentHistory)
                .HasForeignKey(eh => eh.AlumniId)
                .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MatchResult>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(mr => mr.Student)
                .WithMany(s => s.MatchResults)
                .HasForeignKey(mr => mr.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(mr => mr.JobPosting)
                .WithMany(jp => jp.MatchResults)
                .HasForeignKey(mr => mr.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.StudentId, e.JobPostingId }).IsUnique();
            });

            modelBuilder.Entity<PlacementRecord>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(pr => pr.Student)
                .WithMany(s => s.PlacementRecords)
                .HasForeignKey(pr => pr.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pr => pr.JobPosting)
                .WithMany(jp => jp.PlacementRecords)
                .HasForeignKey(pr => pr.JobPostingId)
                .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => new { e.StudentId, e.JobPostingId }).IsUnique();
            });
        }
    }
}
