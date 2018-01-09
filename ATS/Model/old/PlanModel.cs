namespace ATS.Model
{
    using System;
    using System.Data.Entity;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Linq;

    public partial class PlanModel : DbContext
    {
        public PlanModel()
            : base("name=PlanModel")
        {
        }

        public virtual DbSet<plan> plan { get; set; }
        public virtual DbSet<planitem> planitem { get; set; }
        public virtual DbSet<selectjiao> selectjiao { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<plan>()
                .Property(e => e.JiaoName)
                .IsUnicode(false);

            modelBuilder.Entity<plan>()
                .Property(e => e.StaName)
                .IsUnicode(false);

            modelBuilder.Entity<planitem>()
                .Property(e => e.PlanName)
                .IsUnicode(false);

            modelBuilder.Entity<selectjiao>()
                .Property(e => e.JiaoName)
                .IsUnicode(false);

            modelBuilder.Entity<selectjiao>()
                .Property(e => e.TrainNum)
                .IsUnicode(false);

            modelBuilder.Entity<selectjiao>()
                .Property(e => e.Dir)
                .IsUnicode(false);

            modelBuilder.Entity<selectjiao>()
                .Property(e => e.RunMode)
                .IsUnicode(false);

            modelBuilder.Entity<selectjiao>()
                .Property(e => e.ReturnMode)
                .IsUnicode(false);
        }
    }
}
