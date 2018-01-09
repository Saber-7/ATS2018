namespace ATS.Model
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("plans.selectjiao")]
    public partial class selectjiao
    {
        public selectjiao()
        {
            plan = new HashSet<plan>();
        }

        [Key]
        public int JiaoId { get; set; }

        public int PlanId { get; set; }

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

        public virtual ICollection<plan> plan { get; set; }

        //public ICollection<plan> plan { get; set; }
        public virtual planitem planitem { get; set; }
    }
}
