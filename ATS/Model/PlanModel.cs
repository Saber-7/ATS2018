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

        public virtual DbSet<act_plan> act_plan { get; set; }
        public virtual DbSet<act_planitem> act_planitem { get; set; }
        public virtual DbSet<act_selectjiao> act_selectjiao { get; set; }
        public virtual DbSet<jiao_1> jiao_1 { get; set; }
        public virtual DbSet<plan> plan { get; set; }
        public virtual DbSet<plan_1> plan_1 { get; set; }
        public virtual DbSet<planitem> planitem { get; set; }
        public virtual DbSet<selectjiao> selectjiao { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<act_plan>()
                .Property(e => e.act_JiaoName)
                .IsUnicode(false);

            modelBuilder.Entity<act_plan>()
                .Property(e => e.act_StaName)
                .IsUnicode(false);

            modelBuilder.Entity<act_planitem>()
                .Property(e => e.act_PlanName)
                .IsUnicode(false);

            modelBuilder.Entity<act_selectjiao>()
                .Property(e => e.act_JiaoName)
                .IsUnicode(false);

            modelBuilder.Entity<act_selectjiao>()
                .Property(e => e.act_TrainNum)
                .IsUnicode(false);

            modelBuilder.Entity<act_selectjiao>()
                .Property(e => e.act_Dir)
                .IsUnicode(false);

            modelBuilder.Entity<act_selectjiao>()
                .Property(e => e.act_RunMode)
                .IsUnicode(false);

            modelBuilder.Entity<act_selectjiao>()
                .Property(e => e.act_ReturnMode)
                .IsUnicode(false);

            modelBuilder.Entity<act_selectjiao>()
                .Property(e => e.act_TrainGroupNum)
                .IsUnicode(false);

            modelBuilder.Entity<act_selectjiao>()
                .Property(e => e.act_StartSection)
                .IsUnicode(false);

            modelBuilder.Entity<act_selectjiao>()
                .Property(e => e.act_EndSection)
                .IsUnicode(false);

            modelBuilder.Entity<act_selectjiao>()
                .Property(e => e.act_StartSection2)
                .IsUnicode(false);

            modelBuilder.Entity<act_selectjiao>()
                .Property(e => e.act_EndSection2)
                .IsUnicode(false);

            modelBuilder.Entity<jiao_1>()
                .Property(e => e.JiaoName)
                .IsUnicode(false);

            modelBuilder.Entity<jiao_1>()
                .Property(e => e.TrainNum)
                .IsUnicode(false);

            modelBuilder.Entity<jiao_1>()
                .Property(e => e.Dir)
                .IsUnicode(false);

            modelBuilder.Entity<jiao_1>()
                .Property(e => e.RunMode)
                .IsUnicode(false);

            modelBuilder.Entity<jiao_1>()
                .Property(e => e.ReturnMode)
                .IsUnicode(false);

            modelBuilder.Entity<jiao_1>()
                .Property(e => e.TrainGroupNum)
                .IsUnicode(false);

            modelBuilder.Entity<jiao_1>()
                .Property(e => e.StartSection)
                .IsUnicode(false);

            modelBuilder.Entity<jiao_1>()
                .Property(e => e.EndSection)
                .IsUnicode(false);

            modelBuilder.Entity<jiao_1>()
                .Property(e => e.StartSection2)
                .IsUnicode(false);

            modelBuilder.Entity<jiao_1>()
                .Property(e => e.EndSection2)
                .IsUnicode(false);

            modelBuilder.Entity<plan>()
                .Property(e => e.JiaoName)
                .IsUnicode(false);

            modelBuilder.Entity<plan>()
                .Property(e => e.StaName)
                .IsUnicode(false);

            modelBuilder.Entity<plan_1>()
                .Property(e => e.JiaoName)
                .IsUnicode(false);

            modelBuilder.Entity<plan_1>()
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

            modelBuilder.Entity<selectjiao>()
                .Property(e => e.TrainGroupNum)
                .IsUnicode(false);

            modelBuilder.Entity<selectjiao>()
                .Property(e => e.StartSection)
                .IsUnicode(false);

            modelBuilder.Entity<selectjiao>()
                .Property(e => e.EndSection)
                .IsUnicode(false);

            modelBuilder.Entity<selectjiao>()
                .Property(e => e.StartSection2)
                .IsUnicode(false);

            modelBuilder.Entity<selectjiao>()
                .Property(e => e.EndSection2)
                .IsUnicode(false);
        }
    }
}
