namespace ATS.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("plans.jiao_1")]
    public partial class jiao_1
    {
        public jiao_1()
        {
            plan_1 = new HashSet<plan_1>();
        }

        [Key]
        public int JiaoId { get; set; }

        public int? PlanId { get; set; }

        [StringLength(45)]
        public string JiaoName { get; set; }

        [StringLength(45)]
        public string TrainNum { get; set; }

        [Column(TypeName = "enum")]
        [Required]
        [StringLength(65532)]
        public string Dir { get; set; }

        [Column(TypeName = "enum")]
        [Required]
        [StringLength(65532)]
        public string RunMode { get; set; }

        [Column(TypeName = "enum")]
        [Required]
        [StringLength(65532)]
        public string ReturnMode { get; set; }

        [StringLength(45)]
        public string TrainGroupNum { get; set; }

        [StringLength(45)]
        public string StartSection { get; set; }

        [StringLength(45)]
        public string EndSection { get; set; }

        [StringLength(45)]
        public string StartSection2 { get; set; }

        [StringLength(45)]
        public string EndSection2 { get; set; }

        public virtual ICollection<plan_1> plan_1 { get; set; }
    }
}
