namespace ATS.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("plans.act_selectjiao")]
    public partial class act_selectjiao
    {
        public act_selectjiao()
        {
            act_plan = new HashSet<act_plan>();
        }

        [Key]
        public int act_JiaoId { get; set; }

        public int act_PlanId { get; set; }

        [StringLength(45)]
        public string act_JiaoName { get; set; }

        [StringLength(45)]
        public string act_TrainNum { get; set; }

        [Column(TypeName = "enum")]
        [Required]
        [StringLength(65532)]
        public string act_Dir { get; set; }

        [Column(TypeName = "enum")]
        [Required]
        [StringLength(65532)]
        public string act_RunMode { get; set; }

        [Column(TypeName = "enum")]
        [Required]
        [StringLength(65532)]
        public string act_ReturnMode { get; set; }

        [StringLength(45)]
        public string act_TrainGroupNum { get; set; }

        [StringLength(45)]
        public string act_StartSection { get; set; }

        [StringLength(45)]
        public string act_EndSection { get; set; }

        [StringLength(45)]
        public string act_StartSection2 { get; set; }

        [StringLength(45)]
        public string act_EndSection2 { get; set; }

        public virtual ICollection<act_plan> act_plan { get; set; }

        public virtual act_planitem act_planitem { get; set; }
    }
}
